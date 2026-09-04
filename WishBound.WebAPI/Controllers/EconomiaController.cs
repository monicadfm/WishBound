using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;
using WishBound.WebAPI.Services;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Economia virtual: carteiras, recompensa de login diário, eventos de
    /// recompensas e histórico de transações.
    ///
    /// GANHAR moeda:
    ///   - Recompensa diária: calendário de 28 dias (ver CalendarioRecompensaDiaria)
    ///     — Moedas a subir de semana para semana e, na última semana, Bilhetes
    ///     de invocação. Uma por dia; faltar não faz perder o progresso.
    ///   - Eventos: banners de tipo "Evento" com linhas em RecompensasEvento
    ///     (uma por dia). O utilizador resgata um dia de cada vez, um por dia
    ///     de calendário, enquanto o evento estiver a decorrer.
    ///   - Libertar cópias repetidas (ColecaoController).
    ///
    /// GASTAR moeda: invocações (InvocacoesController — Bilhetes primeiro,
    /// depois Moedas) e expansões do inventário (ColecaoController).
    ///
    /// Todos os movimentos ficam em TransacoesMoeda; os saldos são alterados
    /// com UPDATEs condicionais e as "uma vez por dia" são garantidas por
    /// UPDATEs com a data na condição — dois cliques ao mesmo tempo não
    /// recebem duas vezes.
    ///
    /// O "dia" é o dia UTC (a base de dados guarda tudo em UTC).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EconomiaController : ControllerBase
    {
        /// <summary>Tem de ser igual ao de InvocacoesController.</summary>
        private const int CustoInvocacao = 10;

        private readonly WishBoundContext _contexto;

        public EconomiaController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // ------------------------------------------------------------
        // GET: api/economia?utilizadorId=5
        // ------------------------------------------------------------
        // Saldos, estado da recompensa diária e eventos a decorrer, com a
        // participação do utilizador em cada um.
        [HttpGet]
        public async Task<ActionResult<EconomiaResposta>> ObterEconomia([FromQuery] int utilizadorId)
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

                var nomesMoeda = await ObterNomesMoedaAsync();
                var hoje = HojeUtc();

                // ----- Carteiras (uma por tipo de moeda; 0 se ainda não existir) -----
                var carteiras = await _contexto.Carteiras.AsNoTracking()
                    .Where(c => c.UtilizadorId == utilizadorId)
                    .ToListAsync();

                var resposta = new EconomiaResposta { CustoInvocacao = CustoInvocacao };

                foreach (var tipo in nomesMoeda.OrderBy(t => t.Key))
                {
                    resposta.Carteiras.Add(new CarteiraResposta
                    {
                        TipoMoedaId = tipo.Key,
                        Nome = tipo.Value,
                        Saldo = carteiras.FirstOrDefault(c => c.TipoMoedaId == tipo.Key)?.Saldo ?? 0m
                    });
                }

                resposta.SaldoMoedas = carteiras.FirstOrDefault(c => c.TipoMoedaId == TiposMoedaIds.Moedas)?.Saldo ?? 0m;
                resposta.SaldoBilhetes = carteiras.FirstOrDefault(c => c.TipoMoedaId == TiposMoedaIds.Bilhetes)?.Saldo ?? 0m;

                // ----- Recompensa diária -----
                bool recebidaHoje = utilizador.UltimoLoginDiario == hoje;
                int diasRecebidos = utilizador.DiaRecompensaDiaria;

                // Ciclo terminado e ainda não recebeu hoje: recomeça do dia 1
                if (diasRecebidos >= CalendarioRecompensaDiaria.TotalDias && !recebidaHoje)
                {
                    diasRecebidos = 0;
                }

                resposta.RecompensaDiaria = new RecompensaDiariaResposta
                {
                    DiasRecebidos = diasRecebidos,
                    RecebidaHoje = recebidaHoje,
                    ProximoDia = ProximoDia(diasRecebidos, recebidaHoje),
                    Calendario = CalendarioRecompensaDiaria.Construir(diasRecebidos, recebidaHoje,
                        id => nomesMoeda.TryGetValue(id, out var nome) ? nome : "?")
                };

                // ----- Eventos a decorrer -----
                var agora = DateTime.UtcNow;

                var eventos = await _contexto.Banners.AsNoTracking()
                    .Where(b => b.TipoBanner == Banner.TipoEvento && b.IsAtivo &&
                                b.DataInicio <= agora && b.DataFim >= agora)
                    .OrderBy(b => b.DataFim)
                    .ToListAsync();

                foreach (var evento in eventos)
                {
                    var recompensas = await _contexto.RecompensasEvento.AsNoTracking()
                        .Where(r => r.BannerId == evento.Id)
                        .OrderBy(r => r.Id)
                        .ToListAsync();

                    // Eventos sem recompensas são só banners de invocação
                    if (recompensas.Count == 0)
                    {
                        continue;
                    }

                    var participacao = await _contexto.ParticipacoesEventos.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.UtilizadorId == utilizadorId && p.BannerId == evento.Id);

                    int progresso = participacao?.Progresso ?? 0;
                    bool recebidoHoje = participacao != null &&
                                        DateOnly.FromDateTime(participacao.DataParticipacao) == hoje;
                    bool concluido = progresso >= recompensas.Count;
                    int diaDeHoje = recebidoHoje ? progresso : progresso + 1;

                    var eventoResposta = new EventoResposta
                    {
                        BannerId = evento.Id,
                        Nome = evento.Nome,
                        Descricao = evento.Descricao,
                        DataInicio = evento.DataInicio,
                        DataFim = evento.DataFim,
                        DiasRestantes = Math.Max(0, (int)Math.Floor((evento.DataFim - agora).TotalDays)),
                        Progresso = progresso,
                        RecebidoHoje = recebidoHoje,
                        Concluido = concluido,
                        TotalRecompensas = recompensas.Sum(r => r.QuantidadeMoeda ?? 0m)
                    };

                    for (int i = 0; i < recompensas.Count; i++)
                    {
                        var recompensa = recompensas[i];
                        int dia = i + 1;

                        eventoResposta.Dias.Add(new DiaEvento
                        {
                            Dia = dia,
                            Descricao = recompensa.Descricao,
                            TipoMoedaId = recompensa.TipoMoedaId,
                            MoedaNome = recompensa.TipoMoedaId.HasValue &&
                                        nomesMoeda.TryGetValue(recompensa.TipoMoedaId.Value, out var nome) ? nome : string.Empty,
                            Quantidade = recompensa.QuantidadeMoeda ?? 0m,
                            Recebido = dia <= progresso,
                            Hoje = !concluido && dia == diaDeHoje
                        });
                    }

                    resposta.Eventos.Add(eventoResposta);
                }

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a economia do utilizador: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/economia/transacoes?utilizadorId=5&limite=100
        // ------------------------------------------------------------
        [HttpGet("transacoes")]
        public async Task<ActionResult<IEnumerable<TransacaoResposta>>> ObterTransacoes(
            [FromQuery] int utilizadorId, [FromQuery] int limite = 100)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                limite = Math.Clamp(limite, 1, 500);

                var nomesMoeda = await ObterNomesMoedaAsync();

                var transacoes = await _contexto.TransacoesMoeda.AsNoTracking()
                    .Where(t => t.UtilizadorId == utilizadorId)
                    .OrderByDescending(t => t.DataCriacao)
                    .ThenByDescending(t => t.Id)
                    .Take(limite)
                    .ToListAsync();

                var resposta = transacoes.Select(t => new TransacaoResposta
                {
                    Id = t.Id,
                    TipoMoedaId = t.TipoMoedaId,
                    MoedaNome = nomesMoeda.TryGetValue(t.TipoMoedaId, out var nome) ? nome : "?",
                    Montante = t.Montante,
                    TipoTransacao = t.TipoTransacao,
                    Origem = t.Origem,
                    Data = t.DataCriacao
                });

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o histórico de transações: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/economia/login-diario
        // ------------------------------------------------------------
        // Recebe a recompensa do dia do calendário. Uma vez por dia (UTC).
        [HttpPost("login-diario")]
        public async Task<ActionResult<RecompensaRecebidaResposta>> ReceberLoginDiario([FromBody] LoginDiarioPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == pedido.UtilizadorId && u.IsAtivo);

                if (utilizador == null)
                {
                    return BadRequest("Utilizador inválido.");
                }

                var hoje = HojeUtc();

                if (utilizador.UltimoLoginDiario == hoje)
                {
                    return BadRequest("Já recebeste a recompensa diária de hoje. Volta amanhã!");
                }

                int diaAnterior = utilizador.DiaRecompensaDiaria;
                int novoDia = diaAnterior >= CalendarioRecompensaDiaria.TotalDias ? 1 : diaAnterior + 1;

                var (tipoMoedaId, quantidade, semana, fimDeSemana) = CalendarioRecompensaDiaria.Obter(novoDia);

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                // A condição do UPDATE é a garantia de "uma vez por dia": se
                // outro pedido tiver passado primeiro, a data já é hoje (ou o
                // dia já mudou) e este não altera nada.
                int marcadas = await _contexto.Database.ExecuteSqlAsync(
                    $@"UPDATE Utilizadores
                       SET UltimoLoginDiario = {hoje}, DiaRecompensaDiaria = {novoDia}
                       WHERE UtilizadorId = {utilizador.Id}
                         AND DiaRecompensaDiaria = {diaAnterior}
                         AND (UltimoLoginDiario IS NULL OR UltimoLoginDiario < {hoje})");

                if (marcadas == 0)
                {
                    return BadRequest("Já recebeste a recompensa diária de hoje. Volta amanhã!");
                }

                string origem = "Login diario (dia " + novoDia + ")";
                decimal novoSaldo = await CreditarAsync(utilizador.Id, tipoMoedaId, quantidade, origem);

                await transacao.CommitAsync();

                string nomeMoeda = await NomeMoedaAsync(tipoMoedaId);

                string mensagem = "Dia " + novoDia + " de " + CalendarioRecompensaDiaria.TotalDias +
                                  " (semana " + semana + "): recebeste " + FormatarQuantidade(quantidade, nomeMoeda) + "!";
                if (fimDeSemana)
                {
                    mensagem += " Bónus de fim de semana.";
                }
                if (novoDia == CalendarioRecompensaDiaria.TotalDias)
                {
                    mensagem += " Completaste o calendário — amanhã recomeça do dia 1.";
                }

                return Ok(new RecompensaRecebidaResposta
                {
                    Mensagem = mensagem,
                    TipoMoedaId = tipoMoedaId,
                    MoedaNome = nomeMoeda,
                    Quantidade = quantidade,
                    NovoSaldo = novoSaldo,
                    Dia = novoDia
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao receber a recompensa diária: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/economia/evento/resgatar
        // ------------------------------------------------------------
        // Recebe a recompensa do dia seguinte de um evento a decorrer.
        // Um dia por dia de calendário (UTC); faltar um dia não faz perder
        // o progresso, mas o evento acaba na data marcada.
        [HttpPost("evento/resgatar")]
        public async Task<ActionResult<RecompensaRecebidaResposta>> ResgatarEvento([FromBody] ResgatarEventoPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == pedido.UtilizadorId && u.IsAtivo);

                if (utilizador == null)
                {
                    return BadRequest("Utilizador inválido.");
                }

                var agora = DateTime.UtcNow;
                var hoje = HojeUtc();

                var evento = await _contexto.Banners.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == pedido.BannerId && b.TipoBanner == Banner.TipoEvento &&
                                              b.IsAtivo && b.DataInicio <= agora && b.DataFim >= agora);

                if (evento == null)
                {
                    return BadRequest("Este evento não existe ou já não está a decorrer.");
                }

                var recompensas = await _contexto.RecompensasEvento.AsNoTracking()
                    .Where(r => r.BannerId == evento.Id)
                    .OrderBy(r => r.Id)
                    .ToListAsync();

                if (recompensas.Count == 0)
                {
                    return BadRequest("Este evento não tem recompensas para resgatar.");
                }

                int totalDias = recompensas.Count;

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                int diaRecebido;

                // Primeira participação: a própria inserção é o resgate do dia 1.
                // INSERT condicional — se dois pedidos chegarem juntos, só um insere.
                int inseridas = await _contexto.Database.ExecuteSqlAsync(
                    $@"INSERT INTO ParticipacaoEventos (UtilizadorId, BannerId, Progresso, RecompensasResgatadas, DataParticipacao)
                       SELECT {utilizador.Id}, {evento.Id}, 1, {(totalDias <= 1 ? 1 : 0)}, {agora}
                       WHERE NOT EXISTS (SELECT 1 FROM ParticipacaoEventos
                                         WHERE UtilizadorId = {utilizador.Id} AND BannerId = {evento.Id})");

                if (inseridas == 1)
                {
                    diaRecebido = 1;
                }
                else
                {
                    var participacao = await _contexto.ParticipacoesEventos.AsNoTracking()
                        .FirstAsync(p => p.UtilizadorId == utilizador.Id && p.BannerId == evento.Id);

                    if (participacao.Progresso >= totalDias)
                    {
                        return BadRequest("Já recebeste todas as recompensas deste evento.");
                    }

                    if (DateOnly.FromDateTime(participacao.DataParticipacao) == hoje)
                    {
                        return BadRequest("Já recebeste a recompensa de hoje deste evento. Volta amanhã!");
                    }

                    int progressoAnterior = participacao.Progresso;
                    diaRecebido = progressoAnterior + 1;

                    // Só avança se ninguém avançou entretanto e o último resgate
                    // foi noutro dia — é isto que impede dois resgates no mesmo dia.
                    int avancadas = await _contexto.Database.ExecuteSqlAsync(
                        $@"UPDATE ParticipacaoEventos
                           SET Progresso = {diaRecebido},
                               RecompensasResgatadas = {(diaRecebido >= totalDias ? 1 : 0)},
                               DataParticipacao = {agora}
                           WHERE ParticipacaoId = {participacao.Id}
                             AND Progresso = {progressoAnterior}
                             AND CAST(DataParticipacao AS date) < {hoje}");

                    if (avancadas == 0)
                    {
                        return BadRequest("Já recebeste a recompensa de hoje deste evento. Volta amanhã!");
                    }
                }

                var recompensa = recompensas[diaRecebido - 1];

                if (!recompensa.TipoMoedaId.HasValue || !recompensa.QuantidadeMoeda.HasValue || recompensa.QuantidadeMoeda <= 0)
                {
                    // Recompensas de personagem ficam para a funcionalidade de eventos
                    return BadRequest("A recompensa do dia " + diaRecebido + " ainda não é suportada.");
                }

                // Origem tem 50 caracteres na base de dados
                string origem = "Evento: " + evento.Nome;
                string sufixo = " (dia " + diaRecebido + ")";
                if (origem.Length + sufixo.Length > 50)
                {
                    origem = origem.Substring(0, 50 - sufixo.Length - 1) + "…";
                }
                origem += sufixo;

                decimal novoSaldo = await CreditarAsync(utilizador.Id, recompensa.TipoMoedaId.Value,
                    recompensa.QuantidadeMoeda.Value, origem);

                await transacao.CommitAsync();

                string nomeMoeda = await NomeMoedaAsync(recompensa.TipoMoedaId.Value);

                string mensagem = evento.Nome + " — dia " + diaRecebido + " de " + totalDias + ": recebeste " +
                                  FormatarQuantidade(recompensa.QuantidadeMoeda.Value, nomeMoeda) + "!";
                if (diaRecebido >= totalDias)
                {
                    mensagem += " Recebeste todas as recompensas do evento.";
                }

                return Ok(new RecompensaRecebidaResposta
                {
                    Mensagem = mensagem,
                    TipoMoedaId = recompensa.TipoMoedaId.Value,
                    MoedaNome = nomeMoeda,
                    Quantidade = recompensa.QuantidadeMoeda.Value,
                    NovoSaldo = novoSaldo,
                    Dia = diaRecebido
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao resgatar a recompensa do evento: " + ex.Message);
            }
        }

        // ============================================================
        //  Métodos auxiliares
        // ============================================================

        private static DateOnly HojeUtc() => DateOnly.FromDateTime(DateTime.UtcNow);

        private static int ProximoDia(int diasRecebidos, bool recebidaHoje)
        {
            if (!recebidaHoje)
            {
                return diasRecebidos + 1;
            }

            return diasRecebidos >= CalendarioRecompensaDiaria.TotalDias ? 1 : diasRecebidos + 1;
        }

        private static string FormatarQuantidade(decimal quantidade, string nomeMoeda)
        {
            string q = quantidade.ToString("0");

            // "1 Bilhete" / "2 Bilhetes" / "20 Moedas" / "1 Moeda"
            if (quantidade == 1 && nomeMoeda.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                return q + " " + nomeMoeda.Substring(0, nomeMoeda.Length - 1);
            }

            return q + " " + nomeMoeda;
        }

        /// <summary>
        /// Soma a quantidade à carteira (criando-a se faltar) e regista o
        /// movimento como "Ganho". Devolve o novo saldo. Deve correr dentro
        /// da transação de quem chama.
        /// </summary>
        private async Task<decimal> CreditarAsync(int utilizadorId, int tipoMoedaId, decimal quantidade, string origem)
        {
            await _contexto.Database.ExecuteSqlAsync(
                $@"INSERT INTO CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
                   SELECT {utilizadorId}, {tipoMoedaId}, 0
                   WHERE NOT EXISTS (SELECT 1 FROM CarteirasUtilizador
                                     WHERE UtilizadorId = {utilizadorId} AND TipoMoedaId = {tipoMoedaId})");

            await _contexto.Database.ExecuteSqlAsync(
                $@"UPDATE CarteirasUtilizador SET Saldo = Saldo + {quantidade}
                   WHERE UtilizadorId = {utilizadorId} AND TipoMoedaId = {tipoMoedaId}");

            _contexto.TransacoesMoeda.Add(new TransacaoMoeda
            {
                UtilizadorId = utilizadorId,
                TipoMoedaId = tipoMoedaId,
                Montante = quantidade,
                TipoTransacao = TransacaoMoeda.TipoGanho,
                Origem = origem,
                DataCriacao = DateTime.UtcNow
            });

            await _contexto.SaveChangesAsync();

            return await _contexto.Carteiras.AsNoTracking()
                .Where(c => c.UtilizadorId == utilizadorId && c.TipoMoedaId == tipoMoedaId)
                .Select(c => c.Saldo)
                .FirstOrDefaultAsync();
        }

        private async Task<Dictionary<int, string>> ObterNomesMoedaAsync()
        {
            return await _contexto.TiposMoeda.AsNoTracking()
                .ToDictionaryAsync(t => t.Id, t => t.Nome);
        }

        private async Task<string> NomeMoedaAsync(int tipoMoedaId)
        {
            var nome = await _contexto.TiposMoeda.AsNoTracking()
                .Where(t => t.Id == tipoMoedaId)
                .Select(t => t.Nome)
                .FirstOrDefaultAsync();

            return nome ?? "?";
        }
    }
}
