using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;
using WishBound.WebAPI.Services;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Sistema de amizade — nível de amizade por utilizador e por personagem.
    ///
    ///   GET  api/amizade?utilizadorId=              - resumo: interações de hoje,
    ///                                                 níveis e recompensas já ganhas
    ///                                                 (para o perfil)
    ///   GET  api/amizade/personagem?utilizadorId=   - detalhe da amizade com UMA
    ///                                &personagemId=   personagem (página de detalhes)
    ///   POST api/amizade/interagir                  - gasta 1 das 3 interações do dia
    ///   POST api/amizade/titulo                     - título do perfil
    ///   POST api/amizade/moldura                    - moldura do perfil
    ///   POST api/amizade/emblema                    - põe/tira um emblema do perfil (até 3)
    ///
    /// As regras (pontos, nível máximo por raridade, recompensas por nível)
    /// estão no ServicoAmizade; os pontos por cópias repetidas são dados
    /// pelo InvocacoesController.
    ///
    /// INTERAÇÕES: 3 por dia (dia UTC), para gastar em qualquer personagem —
    /// até as 3 na mesma. A contagem é feita com um UPDATE condicional na
    /// linha do utilizador (repõe as 3 quando muda o dia e tira 1), por isso
    /// dois cliques ao mesmo tempo não conseguem gastar a mesma interação.
    /// Na primeira interação de cada dia, as personagens de nível 4+ deixam
    /// a sua mensagem diária (Notificacoes).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AmizadeController : ControllerBase
    {
        private readonly WishBoundContext _contexto;
        private readonly ServicoAmizade _amizade;
        private readonly ServicoMensagens _mensagens;

        public AmizadeController(WishBoundContext contexto, ServicoAmizade amizade, ServicoMensagens mensagens)
        {
            _contexto = contexto;
            _amizade = amizade;
            _mensagens = mensagens;
        }

        // ------------------------------------------------------------
        // GET: api/amizade?utilizadorId=5   (resumo para o perfil)
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<AmizadeResposta>> ObterAmizade([FromQuery] int utilizadorId)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                var utilizador = await _contexto.Utilizadores.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == utilizadorId && u.IsAtivo);

                if (utilizador == null)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                var niveis = await _amizade.ObterNiveisAsync();
                var nomesNiveis = niveis.ToDictionary(n => n.Id, n => n.Nome);
                var ordemNiveis = niveis.ToDictionary(n => n.Id, n => n.Ordem);
                var nomesPersonagens = await _contexto.Personagens.AsNoTracking()
                    .ToDictionaryAsync(p => p.Id, p => p.Nome);

                var resposta = new AmizadeResposta
                {
                    InteracoesRestantes = InteracoesDisponiveis(utilizador, HojeUtc()),
                    InteracoesPorDia = ServicoAmizade.InteracoesPorDia,
                    PontosPorInteracao = ServicoAmizade.PontosPorInteracao,
                    PontosPorRepetida = ServicoAmizade.TabelaPontosPorRepetida(),
                    MultiplicadorEvento = ServicoAmizade.MultiplicadorEvento,
                    MaximoEmblemasEquipados = ServicoAmizade.MaximoEmblemasEquipados,
                    Niveis = niveis.Select(n => ParaResposta(n)).ToList()
                };

                // ----- Títulos ganhos -----
                var titulosGanhos = await _contexto.TitulosUtilizador.AsNoTracking()
                    .Where(t => t.UtilizadorId == utilizadorId)
                    .Join(_contexto.Titulos, tu => tu.TituloId, t => t.Id, (tu, t) => new { tu.DataObtencao, Titulo = t })
                    .ToListAsync();

                foreach (var x in titulosGanhos.OrderBy(y => y.Titulo.PersonagemId).ThenBy(y => OrdemDe(y.Titulo.NivelAmizadeId, ordemNiveis)))
                {
                    resposta.Titulos.Add(ParaResposta(x.Titulo, nomesPersonagens, nomesNiveis, ordemNiveis,
                        obtido: true, data: x.DataObtencao, equipado: utilizador.TituloAtualId == x.Titulo.Id));
                }

                // ----- Emblemas ganhos (com o que está no perfil) -----
                var emblemasGanhos = await _contexto.EmblemasUtilizador.AsNoTracking()
                    .Where(e => e.UtilizadorId == utilizadorId)
                    .Join(_contexto.Emblemas, eu => eu.EmblemaId, e => e.Id, (eu, e) => new { eu.DataObtencao, eu.IsEquipado, Emblema = e })
                    .ToListAsync();

                foreach (var x in emblemasGanhos.OrderByDescending(y => y.IsEquipado).ThenBy(y => y.Emblema.PersonagemId))
                {
                    resposta.Emblemas.Add(ParaResposta(x.Emblema, nomesPersonagens, nomesNiveis,
                        obtido: true, data: x.DataObtencao, equipado: x.IsEquipado));
                }

                // ----- Molduras ganhas -----
                var moldurasGanhas = await _contexto.MoldurasUtilizador.AsNoTracking()
                    .Where(m => m.UtilizadorId == utilizadorId)
                    .Join(_contexto.MoldurasPerfil, mu => mu.MolduraId, m => m.Id, (mu, m) => new { mu.DataObtencao, Moldura = m })
                    .ToListAsync();

                foreach (var x in moldurasGanhas.OrderBy(y => y.Moldura.PersonagemId))
                {
                    resposta.Molduras.Add(ParaResposta(x.Moldura, nomesPersonagens, nomesNiveis,
                        obtida: true, data: x.DataObtencao, equipada: utilizador.MolduraPerfilAtualId == x.Moldura.Id));
                }

                // ----- O que está equipado -----
                var tituloAtual = resposta.Titulos.FirstOrDefault(t => t.Equipado);
                if (tituloAtual != null)
                {
                    resposta.TituloAtualId = tituloAtual.Id;
                    resposta.TituloAtualNome = tituloAtual.Nome;
                    resposta.TituloAtualCor = tituloAtual.CorHex;
                }

                var molduraAtual = resposta.Molduras.FirstOrDefault(m => m.Equipada);
                if (molduraAtual != null)
                {
                    resposta.MolduraAtualId = molduraAtual.Id;
                    resposta.MolduraAtualNome = molduraAtual.Nome;
                    resposta.MolduraAtualCor = molduraAtual.CorHex;
                }

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o sistema de amizade: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/amizade/personagem?utilizadorId=5&personagemId=3
        // ------------------------------------------------------------
        // Tudo o que a página de detalhes da personagem mostra por baixo da
        // descrição: progresso, níveis (com o que cada um dá e se já foi
        // atingido / é alcançável para esta raridade) e as recompensas
        // desta personagem com o estado.
        [HttpGet("personagem")]
        public async Task<ActionResult<AmizadePersonagemDetalhe>> ObterPersonagem(
            [FromQuery] int utilizadorId, [FromQuery] int personagemId)
        {
            try
            {
                if (utilizadorId <= 0 || personagemId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador e a personagem.");
                }

                var utilizador = await _contexto.Utilizadores.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == utilizadorId && u.IsAtivo);

                if (utilizador == null)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                var item = await _contexto.Colecoes.AsNoTracking()
                    .Include(c => c.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .FirstOrDefaultAsync(c => c.UtilizadorId == utilizadorId && c.PersonagemId == personagemId);

                if (item == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                var niveis = await _amizade.ObterNiveisAsync();
                var nomesNiveis = niveis.ToDictionary(n => n.Id, n => n.Nome);
                var ordemNiveis = niveis.ToDictionary(n => n.Id, n => n.Ordem);
                var nomesPersonagens = new Dictionary<int, string> { [personagemId] = item.Personagem?.Nome ?? "?" };

                var basico = ParaResposta(item, niveis);

                var detalhe = new AmizadePersonagemDetalhe
                {
                    PersonagemId = basico.PersonagemId,
                    Nome = basico.Nome,
                    ImagemUrl = basico.ImagemUrl,
                    RaridadeNome = basico.RaridadeNome,
                    RaridadeCor = basico.RaridadeCor,
                    RaridadeOrdem = basico.RaridadeOrdem,
                    PontosAmizade = basico.PontosAmizade,
                    NivelAmizadeId = basico.NivelAmizadeId,
                    NivelAmizadeNome = basico.NivelAmizadeNome,
                    NivelOrdem = basico.NivelOrdem,
                    NivelMaximoOrdem = basico.NivelMaximoOrdem,
                    NivelMaximoNome = basico.NivelMaximoNome,
                    PontosNivelAtual = basico.PontosNivelAtual,
                    PontosProximoNivel = basico.PontosProximoNivel,
                    UltimaInteracao = basico.UltimaInteracao,
                    InteracoesRestantes = InteracoesDisponiveis(utilizador, HojeUtc()),
                    InteracoesPorDia = ServicoAmizade.InteracoesPorDia,
                    PontosPorInteracao = ServicoAmizade.PontosPorInteracao,
                    NotificacoesAtivas = basico.NivelOrdem >= ServicoAmizade.NivelNotificacoes,
                    MaximoEmblemasEquipados = ServicoAmizade.MaximoEmblemasEquipados,
                    EmblemasEquipados = await _contexto.EmblemasUtilizador
                        .CountAsync(e => e.UtilizadorId == utilizadorId && e.IsEquipado)
                };

                foreach (var n in niveis)
                {
                    detalhe.Niveis.Add(new NivelPersonagemResposta
                    {
                        Ordem = n.Ordem,
                        Nome = n.Nome,
                        PontosNecessarios = n.PontosNecessarios,
                        Recompensa = ServicoAmizade.DescricaoRecompensa(n.Ordem),
                        Atingido = n.Ordem <= basico.NivelOrdem,
                        Alcancavel = n.Ordem <= basico.NivelMaximoOrdem
                    });
                }

                // ----- Recompensas desta personagem -----
                var titulos = await _contexto.Titulos.AsNoTracking()
                    .Where(t => t.PersonagemId == personagemId)
                    .ToListAsync();

                var titulosGanhos = await _contexto.TitulosUtilizador.AsNoTracking()
                    .Where(t => t.UtilizadorId == utilizadorId)
                    .ToDictionaryAsync(t => t.TituloId, t => t.DataObtencao);

                foreach (var t in titulos)
                {
                    int ordem = OrdemDe(t.NivelAmizadeId, ordemNiveis);
                    bool obtido = titulosGanhos.TryGetValue(t.Id, out var data);
                    var resposta = ParaResposta(t, nomesPersonagens, nomesNiveis, ordemNiveis,
                        obtido, obtido ? data : null, utilizador.TituloAtualId == t.Id);

                    if (ordem >= ServicoAmizade.NivelTituloUnico)
                    {
                        // Só faz sentido mostrar o título único se a raridade lá chegar
                        if (basico.NivelMaximoOrdem >= ServicoAmizade.NivelTituloUnico)
                        {
                            detalhe.TituloUnico = resposta;
                        }
                    }
                    else
                    {
                        detalhe.Titulo = resposta;
                    }
                }

                if (basico.NivelMaximoOrdem >= ServicoAmizade.NivelEmblema)
                {
                    var emblema = await _contexto.Emblemas.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.PersonagemId == personagemId);

                    if (emblema != null)
                    {
                        var ganho = await _contexto.EmblemasUtilizador.AsNoTracking()
                            .FirstOrDefaultAsync(e => e.UtilizadorId == utilizadorId && e.EmblemaId == emblema.Id);

                        detalhe.Emblema = ParaResposta(emblema, nomesPersonagens, nomesNiveis,
                            ganho != null, ganho?.DataObtencao, ganho?.IsEquipado ?? false);
                    }
                }

                if (basico.NivelMaximoOrdem >= ServicoAmizade.NivelMoldura)
                {
                    var moldura = await _contexto.MoldurasPerfil.AsNoTracking()
                        .FirstOrDefaultAsync(m => m.PersonagemId == personagemId);

                    if (moldura != null)
                    {
                        var ganha = await _contexto.MoldurasUtilizador.AsNoTracking()
                            .FirstOrDefaultAsync(m => m.UtilizadorId == utilizadorId && m.MolduraId == moldura.Id);

                        detalhe.Moldura = ParaResposta(moldura, nomesPersonagens, nomesNiveis,
                            ganha != null, ganha?.DataObtencao, utilizador.MolduraPerfilAtualId == moldura.Id);
                    }
                }

                return Ok(detalhe);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a amizade com a personagem: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/amizade/interagir
        // ------------------------------------------------------------
        [HttpPost("interagir")]
        public async Task<ActionResult<InteracaoResposta>> Interagir([FromBody] InteragirPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == pedido.UtilizadorId && u.IsAtivo);

                if (utilizador == null)
                {
                    return BadRequest("Utilizador inválido.");
                }

                var item = await _contexto.Colecoes.AsNoTracking()
                    .Include(c => c.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .FirstOrDefaultAsync(c => c.UtilizadorId == pedido.UtilizadorId && c.PersonagemId == pedido.PersonagemId);

                if (item == null || item.Personagem == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                var hoje = HojeUtc();

                if (InteracoesDisponiveis(utilizador, hoje) <= 0)
                {
                    return BadRequest("Já gastaste as " + ServicoAmizade.InteracoesPorDia + " interações de hoje. Volta amanhã!");
                }

                // Primeira interação de hoje? (as mensagens diárias são enviadas aqui)
                bool primeiraDoDia = utilizador.DiaInteracoes == null || utilizador.DiaInteracoes < hoje;

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                // Gasta a interação: repõe as 3 se o dia mudou e tira 1. A
                // condição do WHERE é a garantia de não gastar a mais.
                // ADMIN: interações ilimitadas — só marca o dia (para as
                // mensagens diárias saírem uma vez por dia) sem descontar.
                int gastas = utilizador.IsAdmin
                    ? await _contexto.Database.ExecuteSqlAsync(
                        $@"UPDATE Utilizadores SET DiaInteracoes = {hoje} WHERE UtilizadorId = {utilizador.Id}")
                    : await _contexto.Database.ExecuteSqlAsync(
                    $@"UPDATE Utilizadores
                       SET InteracoesRestantes =
                               CASE WHEN DiaInteracoes IS NULL OR DiaInteracoes < {hoje}
                                    THEN {ServicoAmizade.InteracoesPorDia}
                                    ELSE InteracoesRestantes END - 1,
                           DiaInteracoes = {hoje}
                       WHERE UtilizadorId = {utilizador.Id}
                         AND (DiaInteracoes IS NULL OR DiaInteracoes < {hoje} OR InteracoesRestantes > 0)");

                if (gastas == 0)
                {
                    return BadRequest("Já gastaste as " + ServicoAmizade.InteracoesPorDia + " interações de hoje. Volta amanhã!");
                }

                var resultado = await _amizade.AdicionarPontosAsync(
                    utilizador.Id, item.PersonagemId, ServicoAmizade.PontosPorInteracao, marcarInteracao: true);

                if (resultado == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                var recompensas = await _amizade.DesbloquearRecompensasAsync(
                    utilizador.Id, item.PersonagemId, item.Personagem.Nome, resultado.NivelAnterior, resultado.NivelAtual);

                int mensagensDiarias = primeiraDoDia ? await _amizade.EnviarMensagensDiariasAsync(utilizador.Id) : 0;

                await transacao.CommitAsync();

                int restantes = utilizador.IsAdmin
                    ? InteracoesAdmin
                    : await _contexto.Utilizadores.AsNoTracking()
                        .Where(u => u.Id == utilizador.Id)
                        .Select(u => u.InteracoesRestantes)
                        .FirstAsync();

                int nivelMaximo = ServicoAmizade.NivelMaximo(item.Personagem.Raridade?.Ordem ?? 1);
                bool noMaximo = resultado.NivelAtual.Ordem >= nivelMaximo;

                string mensagem;
                if (resultado.SubiuDeNivel)
                {
                    mensagem = "A tua amizade com " + item.Personagem.Nome + " subiu para \"" + resultado.NivelAtual.Nome + "\"!";
                    if (recompensas.Count > 0)
                    {
                        mensagem += " Desbloqueaste: " + string.Join(", ", recompensas.Select(ServicoAmizade.DescreverRecompensa)) + ".";
                    }
                    if (noMaximo)
                    {
                        mensagem += " É o nível máximo para uma personagem " + (item.Personagem.Raridade?.Nome ?? "desta raridade") + ".";
                    }
                }
                else if (noMaximo)
                {
                    mensagem = item.Personagem.Nome + " já está no nível máximo (\"" + resultado.NivelAtual.Nome + "\"). Gostou na mesma!";
                }
                else
                {
                    mensagem = "+" + ServicoAmizade.PontosPorInteracao + " pontos de amizade com " + item.Personagem.Nome +
                               " (" + resultado.PontosAmizade + " pontos, nível \"" + resultado.NivelAtual.Nome + "\").";
                }

                mensagem += restantes == 0
                    ? " Foi a última interação de hoje."
                    : " Ainda tens " + restantes + (restantes == 1 ? " interação" : " interações") + " hoje.";

                if (mensagensDiarias > 0)
                {
                    mensagem += " " + mensagensDiarias + (mensagensDiarias == 1 ? " personagem deixou-te uma mensagem." : " personagens deixaram-te mensagens.");
                }

                return Ok(new InteracaoResposta
                {
                    Mensagem = mensagem,
                    Reacao = await ReacaoAsync(item.PersonagemId, item.Personagem.Nome, resultado.NivelAtual.Ordem),
                    PersonagemId = item.PersonagemId,
                    PersonagemNome = item.Personagem.Nome,
                    PontosGanhos = ServicoAmizade.PontosPorInteracao,
                    PontosAmizade = resultado.PontosAmizade,
                    NivelAnterior = resultado.NivelAnterior.Nome,
                    NivelAtual = resultado.NivelAtual.Nome,
                    SubiuDeNivel = resultado.SubiuDeNivel,
                    NivelMaximo = noMaximo,
                    InteracoesRestantes = restantes,
                    Recompensas = recompensas,
                    MensagensDiarias = mensagensDiarias
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao interagir com a personagem: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/amizade/titulo  (escolher o título do perfil)
        // ------------------------------------------------------------
        [HttpPost("titulo")]
        public async Task<IActionResult> EquiparTitulo([FromBody] EquiparTituloPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.FindAsync(pedido.UtilizadorId);
                if (utilizador == null || !utilizador.IsAtivo)
                {
                    return BadRequest("Utilizador inválido.");
                }

                if (pedido.TituloId.HasValue)
                {
                    // Só pode equipar títulos que já ganhou
                    bool ganho = await _contexto.TitulosUtilizador
                        .AnyAsync(t => t.UtilizadorId == utilizador.Id && t.TituloId == pedido.TituloId.Value);

                    if (!ganho)
                    {
                        return BadRequest("Ainda não desbloqueaste esse título.");
                    }
                }

                utilizador.TituloAtualId = pedido.TituloId;
                await _contexto.SaveChangesAsync();

                if (!pedido.TituloId.HasValue)
                {
                    return Ok("Título removido do perfil.");
                }

                var titulo = await _contexto.Titulos.FindAsync(pedido.TituloId.Value);
                return Ok("Título \"" + (titulo?.Nome ?? "?") + "\" equipado no perfil.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao escolher o título: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/amizade/moldura  (escolher a moldura do perfil)
        // ------------------------------------------------------------
        [HttpPost("moldura")]
        public async Task<IActionResult> EquiparMoldura([FromBody] EquiparMolduraPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.FindAsync(pedido.UtilizadorId);
                if (utilizador == null || !utilizador.IsAtivo)
                {
                    return BadRequest("Utilizador inválido.");
                }

                if (pedido.MolduraId.HasValue)
                {
                    bool ganha = await _contexto.MoldurasUtilizador
                        .AnyAsync(m => m.UtilizadorId == utilizador.Id && m.MolduraId == pedido.MolduraId.Value);

                    if (!ganha)
                    {
                        return BadRequest("Ainda não desbloqueaste essa moldura.");
                    }
                }

                utilizador.MolduraPerfilAtualId = pedido.MolduraId;
                await _contexto.SaveChangesAsync();

                if (!pedido.MolduraId.HasValue)
                {
                    return Ok("Moldura removida do perfil.");
                }

                var moldura = await _contexto.MoldurasPerfil.FindAsync(pedido.MolduraId.Value);
                return Ok("Moldura \"" + (moldura?.Nome ?? "?") + "\" equipada no perfil.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao escolher a moldura: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/amizade/emblema  (pôr/tirar um emblema do perfil, até 3)
        // ------------------------------------------------------------
        [HttpPost("emblema")]
        public async Task<IActionResult> EquiparEmblema([FromBody] EquiparEmblemaPedido pedido)
        {
            try
            {
                var ganho = await _contexto.EmblemasUtilizador
                    .FirstOrDefaultAsync(e => e.UtilizadorId == pedido.UtilizadorId && e.EmblemaId == pedido.EmblemaId);

                if (ganho == null)
                {
                    return BadRequest("Ainda não desbloqueaste esse emblema.");
                }

                var emblema = await _contexto.Emblemas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == pedido.EmblemaId);
                string nome = emblema?.Nome ?? "?";

                if (!pedido.Equipar)
                {
                    ganho.IsEquipado = false;
                    await _contexto.SaveChangesAsync();
                    return Ok("Emblema \"" + nome + "\" retirado do perfil.");
                }

                if (ganho.IsEquipado)
                {
                    return Ok("O emblema \"" + nome + "\" já está no perfil.");
                }

                // Limite de 3 no perfil — verificado com um UPDATE condicional
                // para dois cliques rápidos não passarem a 4.
                int equipados = await _contexto.Database.ExecuteSqlAsync(
                    $@"UPDATE EmblemasUtilizador SET IsEquipado = 1
                       WHERE UtilizadorId = {pedido.UtilizadorId} AND EmblemaId = {pedido.EmblemaId} AND IsEquipado = 0
                         AND (SELECT COUNT(*) FROM EmblemasUtilizador
                              WHERE UtilizadorId = {pedido.UtilizadorId} AND IsEquipado = 1) < {ServicoAmizade.MaximoEmblemasEquipados}");

                if (equipados == 0)
                {
                    return BadRequest("Já tens " + ServicoAmizade.MaximoEmblemasEquipados +
                                      " emblemas no perfil. Tira um para pores este.");
                }

                return Ok("Emblema \"" + nome + "\" equipado no perfil.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao escolher o emblema: " + ex.Message);
            }
        }

        // ============================================================
        //  Métodos auxiliares
        // ============================================================

        private static DateOnly HojeUtc() => DateOnly.FromDateTime(DateTime.UtcNow);

        /// <summary>Interações que ainda tem hoje: as 3 se o dia mudou, senão o que sobra.</summary>
        /// <summary>Interações mostradas a um administrador (nunca as gasta).</summary>
        public const int InteracoesAdmin = 99;

        public static int InteracoesDisponiveis(Utilizador u, DateOnly hoje)
        {
            if (u.IsAdmin)
            {
                return InteracoesAdmin;
            }

            return u.DiaInteracoes == null || u.DiaInteracoes < hoje
                ? ServicoAmizade.InteracoesPorDia
                : Math.Max(0, u.InteracoesRestantes);
        }

        private static int OrdemDe(int? nivelId, Dictionary<int, int> ordemNiveis) =>
            nivelId.HasValue && ordemNiveis.TryGetValue(nivelId.Value, out var o) ? o : 0;

        /// <summary>
        /// A "fala" da personagem depois de uma interação: uma mensagem do
        /// conjunto "Aleatoria" da personagem (MensagensPersonagem,
        /// Migracao07) ao nível atual; se a personagem não tiver conjunto,
        /// a frase genérica por nível de <see cref="Reacao"/>.
        /// </summary>
        private async Task<string> ReacaoAsync(int personagemId, string nome, int ordemNivel)
        {
            return await _mensagens.EscolherAsync(personagemId, MensagemPersonagem.TipoAleatoria, ordemNivel)
                   ?? Reacao(nome, ordemNivel);
        }

        /// <summary>Reação GENÉRICA por nível — só para personagens sem conjunto "Aleatoria".</summary>
        private static string Reacao(string nome, int ordemNivel) => ordemNivel switch
        {
            1 => nome + " olha para ti com curiosidade… ainda não sabe bem quem és.",
            2 => nome + " acena-te. Já te reconhece.",
            3 => nome + " sorri: \"Que bom ver-te outra vez!\"",
            4 => nome + " corre ao teu encontro: \"Estava à tua espera!\"",
            5 => nome + " diz baixinho: \"Contigo é diferente.\"",
            6 => nome + " fica em silêncio ao teu lado. Não é preciso dizer nada.",
            _ => nome + " brilha só de te ver. Há laços que não se explicam."
        };

        /// <summary>Estado da amizade com uma personagem (com o próximo nível e o máximo da raridade).</summary>
        public static AmizadePersonagemResposta ParaResposta(ItemColecao item, List<NivelAmizade> niveis)
        {
            var nivel = niveis.FirstOrDefault(n => n.Id == item.NivelAmizadeId) ?? niveis.First();
            int maximoOrdem = ServicoAmizade.NivelMaximo(item.Personagem?.Raridade?.Ordem ?? 1);
            var maximo = niveis.FirstOrDefault(n => n.Ordem == maximoOrdem) ?? niveis.Last();
            var proximo = nivel.Ordem >= maximoOrdem ? null : niveis.FirstOrDefault(n => n.Ordem == nivel.Ordem + 1);

            return new AmizadePersonagemResposta
            {
                PersonagemId = item.PersonagemId,
                Nome = item.Personagem?.Nome ?? "Personagem removida",
                ImagemUrl = item.Personagem?.ImagemUrl,
                RaridadeNome = item.Personagem?.Raridade?.Nome ?? "Desconhecida",
                RaridadeCor = item.Personagem?.Raridade?.Cor,
                RaridadeOrdem = item.Personagem?.Raridade?.Ordem ?? 0,
                PontosAmizade = item.PontosAmizade,
                NivelAmizadeId = nivel.Id,
                NivelAmizadeNome = nivel.Nome,
                NivelOrdem = nivel.Ordem,
                NivelMaximoOrdem = maximo.Ordem,
                NivelMaximoNome = maximo.Nome,
                PontosNivelAtual = nivel.PontosNecessarios,
                PontosProximoNivel = proximo?.PontosNecessarios,
                UltimaInteracao = item.UltimaInteracao
            };
        }

        private static NivelAmizadeResposta ParaResposta(NivelAmizade n) => new NivelAmizadeResposta
        {
            Id = n.Id,
            Nome = n.Nome,
            PontosNecessarios = n.PontosNecessarios,
            Ordem = n.Ordem,
            Recompensa = ServicoAmizade.DescricaoRecompensa(n.Ordem)
        };

        private static TituloResposta ParaResposta(Titulo t, Dictionary<int, string> nomesPersonagens,
            Dictionary<int, string> nomesNiveis, Dictionary<int, int> ordemNiveis, bool obtido, DateTime? data, bool equipado)
        {
            return new TituloResposta
            {
                Id = t.Id,
                Nome = t.Nome,
                Descricao = t.Descricao,
                PersonagemId = t.PersonagemId,
                PersonagemNome = t.PersonagemId.HasValue && nomesPersonagens.TryGetValue(t.PersonagemId.Value, out var np) ? np : null,
                NivelNome = t.NivelAmizadeId.HasValue && nomesNiveis.TryGetValue(t.NivelAmizadeId.Value, out var nn) ? nn : null,
                NivelOrdem = OrdemDe(t.NivelAmizadeId, ordemNiveis),
                IsPersonalizado = t.IsPersonalizado,
                CorHex = t.CorHex,
                Obtido = obtido,
                Equipado = equipado,
                DataObtencao = data
            };
        }

        private static EmblemaResposta ParaResposta(Emblema e, Dictionary<int, string> nomesPersonagens,
            Dictionary<int, string> nomesNiveis, bool obtido, DateTime? data, bool equipado)
        {
            return new EmblemaResposta
            {
                Id = e.Id,
                Nome = e.Nome,
                Descricao = e.Descricao,
                ImagemUrl = e.ImagemUrl,
                PersonagemId = e.PersonagemId,
                PersonagemNome = e.PersonagemId.HasValue && nomesPersonagens.TryGetValue(e.PersonagemId.Value, out var np) ? np : null,
                NivelNome = e.NivelAmizadeId.HasValue && nomesNiveis.TryGetValue(e.NivelAmizadeId.Value, out var nn) ? nn : null,
                Obtido = obtido,
                Equipado = equipado,
                DataObtencao = data
            };
        }

        private static MolduraResposta ParaResposta(MolduraPerfil m, Dictionary<int, string> nomesPersonagens,
            Dictionary<int, string> nomesNiveis, bool obtida, DateTime? data, bool equipada)
        {
            return new MolduraResposta
            {
                Id = m.Id,
                Nome = m.Nome,
                ImagemUrl = m.ImagemUrl,
                CorHex = m.CorHex,
                PersonagemId = m.PersonagemId,
                PersonagemNome = m.PersonagemId.HasValue && nomesPersonagens.TryGetValue(m.PersonagemId.Value, out var np) ? np : null,
                NivelNome = m.NivelAmizadeId.HasValue && nomesNiveis.TryGetValue(m.NivelAmizadeId.Value, out var nn) ? nn : null,
                Obtida = obtida,
                Equipada = equipada,
                DataObtencao = data
            };
        }
    }
}
