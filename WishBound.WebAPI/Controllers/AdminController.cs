using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;
using WishBound.WebAPI.Services;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// ADMINISTRAÇÃO DE CONTAS (api/admin).
    ///
    /// O que um administrador pode fazer a qualquer conta:
    ///   - ver a lista de contas (pesquisa + paginação) e o detalhe de uma
    ///     conta: carteiras, inventário, coleção com a amizade, recompensas
    ///     de amizade (títulos, emblemas, molduras), últimas transações e
    ///     últimas ações de administração sobre ela;
    ///   - ESTADO: ativar/desativar, promover/despromover a administrador,
    ///     marcar o email como validado, repor a password;
    ///   - MOEDA: dar ou tirar Moedas, Bilhetes ou Gemas (fica sempre em
    ///     TransacoesMoeda, como qualquer outro movimento);
    ///   - PERSONAGENS: dar ou tirar cópias de uma personagem; definir a
    ///     capacidade extra do inventário;
    ///   - AMIZADE: definir os pontos de amizade com uma personagem (com as
    ///     mesmas regras de limite e de recompensas do sistema de amizade);
    ///   - RECOMPENSAS: conceder ou revogar títulos, emblemas e molduras.
    ///
    /// REGISTO DE AÇÕES: cada escrita grava uma linha em LogsAdministrador
    /// (quem, a quem, o quê, porquê) DENTRO da transação da ação — não há
    /// ação sem registo nem registo sem ação. GET api/admin/acoes lê o registo.
    ///
    /// SEGURANÇA: todos os pedidos de escrita trazem o AdminId; a API
    /// confirma que é uma conta ativa com IsAdmin. É a mesma fronteira de
    /// confiança do resto da API (chave partilhada + Id enviado pelo site) —
    /// a limitação conhecida, assumida no relatório. Regras extra:
    ///   - um administrador não pode desativar nem despromover a própria
    ///     conta;
    ///   - a última conta de administrador ativa nunca pode ser desativada
    ///     nem despromovida (senão ninguém voltava a entrar na gestão).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private const int CapacidadeBasePadrao = 100;

        private readonly WishBoundContext _contexto;
        private readonly ServicoAmizade _amizade;

        public AdminController(WishBoundContext contexto, ServicoAmizade amizade)
        {
            _contexto = contexto;
            _amizade = amizade;
        }

        // ============================================================
        //  CONSULTAS
        // ============================================================

        // ------------------------------------------------------------
        // GET: api/admin/utilizadores?adminId=1&pesquisa=ana&filtro=ativos&pagina=1&tamanho=20
        // ------------------------------------------------------------
        // filtro: todos | ativos | inativos | admins | porvalidar
        [HttpGet("utilizadores")]
        public async Task<ActionResult<AdminListaUtilizadores>> ObterUtilizadores(
            [FromQuery] int adminId, [FromQuery] string? pesquisa, [FromQuery] string? filtro,
            [FromQuery] int pagina = 1, [FromQuery] int tamanho = 20)
        {
            try
            {
                var admin = await ObterAdminAsync(adminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem consultar as contas.");
                }

                pagina = Math.Max(1, pagina);
                tamanho = Math.Clamp(tamanho, 5, 100);
                pesquisa = pesquisa?.Trim();

                IQueryable<Utilizador> consulta = _contexto.Utilizadores.AsNoTracking();

                if (!string.IsNullOrEmpty(pesquisa))
                {
                    consulta = consulta.Where(u => u.NomeUtilizador.Contains(pesquisa) || u.Email.Contains(pesquisa));
                }

                consulta = (filtro ?? "todos").ToLowerInvariant() switch
                {
                    "ativos" => consulta.Where(u => u.IsAtivo),
                    "inativos" => consulta.Where(u => !u.IsAtivo),
                    "admins" => consulta.Where(u => u.IsAdmin),
                    "porvalidar" => consulta.Where(u => !u.EmailValidado),
                    _ => consulta
                };

                int total = await consulta.CountAsync();

                var utilizadores = await consulta
                    .OrderBy(u => u.Id)
                    .Skip((pagina - 1) * tamanho)
                    .Take(tamanho)
                    .ToListAsync();

                var ids = utilizadores.Select(u => u.Id).ToList();

                // Saldos e tamanho da coleção das contas desta página, em duas consultas
                var carteiras = await _contexto.Carteiras.AsNoTracking()
                    .Where(c => ids.Contains(c.UtilizadorId))
                    .ToListAsync();

                var colecoes = await _contexto.Colecoes.AsNoTracking()
                    .Where(c => ids.Contains(c.UtilizadorId))
                    .GroupBy(c => c.UtilizadorId)
                    .Select(g => new { UtilizadorId = g.Key, Distintas = g.Count(), Copias = g.Sum(c => c.Quantidade) })
                    .ToListAsync();

                var resposta = new AdminListaUtilizadores
                {
                    Total = total,
                    Pagina = pagina,
                    Tamanho = tamanho,
                    TotalContas = await _contexto.Utilizadores.CountAsync(),
                    TotalAtivas = await _contexto.Utilizadores.CountAsync(u => u.IsAtivo),
                    TotalAdmins = await _contexto.Utilizadores.CountAsync(u => u.IsAdmin),
                    TotalPorValidar = await _contexto.Utilizadores.CountAsync(u => !u.EmailValidado)
                };

                foreach (var u in utilizadores)
                {
                    var resumo = ParaResumo(u);
                    resumo.SaldoMoedas = carteiras.FirstOrDefault(c => c.UtilizadorId == u.Id && c.TipoMoedaId == TiposMoedaIds.Moedas)?.Saldo ?? 0m;
                    resumo.SaldoBilhetes = carteiras.FirstOrDefault(c => c.UtilizadorId == u.Id && c.TipoMoedaId == TiposMoedaIds.Bilhetes)?.Saldo ?? 0m;

                    var colecao = colecoes.FirstOrDefault(c => c.UtilizadorId == u.Id);
                    resumo.PersonagensDistintas = colecao?.Distintas ?? 0;
                    resumo.TotalCopias = colecao?.Copias ?? 0;

                    resposta.Itens.Add(resumo);
                }

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a lista de contas: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/admin/utilizadores/5?adminId=1
        // ------------------------------------------------------------
        [HttpGet("utilizadores/{id:int}")]
        public async Task<ActionResult<AdminUtilizadorDetalhe>> ObterUtilizador(int id, [FromQuery] int adminId)
        {
            try
            {
                var admin = await ObterAdminAsync(adminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem consultar as contas.");
                }

                var utilizador = await _contexto.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (utilizador == null)
                {
                    return NotFound("Conta não encontrada.");
                }

                var detalhe = new AdminUtilizadorDetalhe { Conta = ParaResumo(utilizador) };

                // ----- Carteiras (uma por tipo de moeda, 0 se não existir) -----
                var nomesMoeda = await _contexto.TiposMoeda.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Nome);
                var carteiras = await _contexto.Carteiras.AsNoTracking().Where(c => c.UtilizadorId == id).ToListAsync();

                foreach (var tipo in nomesMoeda.OrderBy(t => t.Key))
                {
                    detalhe.Carteiras.Add(new AdminCarteira
                    {
                        TipoMoedaId = tipo.Key,
                        Nome = tipo.Value,
                        Saldo = carteiras.FirstOrDefault(c => c.TipoMoedaId == tipo.Key)?.Saldo ?? 0m
                    });
                }

                detalhe.Conta.SaldoMoedas = carteiras.FirstOrDefault(c => c.TipoMoedaId == TiposMoedaIds.Moedas)?.Saldo ?? 0m;
                detalhe.Conta.SaldoBilhetes = carteiras.FirstOrDefault(c => c.TipoMoedaId == TiposMoedaIds.Bilhetes)?.Saldo ?? 0m;

                // ----- Inventário e coleção -----
                var inventario = await _contexto.Inventarios.AsNoTracking().FirstOrDefaultAsync(i => i.UtilizadorId == id);
                detalhe.CapacidadeBase = inventario?.CapacidadeBase ?? CapacidadeBasePadrao;
                detalhe.CapacidadeExtra = inventario?.CapacidadeExtra ?? 0;

                var niveis = await _amizade.ObterNiveisAsync();

                var itens = await _contexto.Colecoes.AsNoTracking()
                    .Where(c => c.UtilizadorId == id)
                    .Include(c => c.Personagem!).ThenInclude(p => p.Raridade)
                    .OrderByDescending(c => c.Personagem!.Raridade!.Ordem)
                    .ThenBy(c => c.Personagem!.Nome)
                    .ToListAsync();

                foreach (var item in itens)
                {
                    int ordemRaridade = item.Personagem?.Raridade?.Ordem ?? 1;
                    int ordemMaxima = ServicoAmizade.NivelMaximo(ordemRaridade);
                    var nivel = niveis.FirstOrDefault(n => n.Id == item.NivelAmizadeId) ?? niveis.First();
                    var nivelMaximo = niveis.FirstOrDefault(n => n.Ordem == ordemMaxima) ?? niveis.Last();

                    detalhe.Colecao.Add(new AdminItemColecao
                    {
                        PersonagemId = item.PersonagemId,
                        Nome = item.Personagem?.Nome ?? "?",
                        ImagemUrl = item.Personagem?.ImagemUrl,
                        RaridadeNome = item.Personagem?.Raridade?.Nome ?? "-",
                        RaridadeCor = item.Personagem?.Raridade?.Cor,
                        RaridadeOrdem = ordemRaridade,
                        Quantidade = item.Quantidade,
                        IsFavorito = item.IsFavorito,
                        PontosAmizade = item.PontosAmizade,
                        NivelAmizadeNome = nivel.Nome,
                        NivelOrdem = nivel.Ordem,
                        NivelMaximoOrdem = ordemMaxima,
                        PontosNivelMaximo = nivelMaximo.PontosNecessarios,
                        DataObtencao = item.DataObtencao
                    });
                }

                detalhe.Ocupado = itens.Sum(i => i.Quantidade);
                detalhe.Conta.PersonagensDistintas = itens.Count;
                detalhe.Conta.TotalCopias = detalhe.Ocupado;

                var idsNaColecao = itens.Select(i => i.PersonagemId).ToHashSet();

                detalhe.PersonagensDisponiveis = await _contexto.Personagens.AsNoTracking()
                    .Include(p => p.Raridade)
                    .OrderBy(p => p.Raridade!.Ordem).ThenBy(p => p.Nome)
                    .Select(p => new AdminPersonagemOpcao
                    {
                        Id = p.Id,
                        Nome = p.Nome,
                        RaridadeNome = p.Raridade!.Nome,
                        IsAtivo = p.IsAtivo
                    })
                    .ToListAsync();
                detalhe.PersonagensDisponiveis.RemoveAll(p => idsNaColecao.Contains(p.Id));

                // ----- Amizade / economia diária -----
                detalhe.InteracoesRestantes = AmizadeController.InteracoesDisponiveis(utilizador, DateOnly.FromDateTime(DateTime.UtcNow));
                detalhe.DiaRecompensaDiaria = utilizador.DiaRecompensaDiaria;
                detalhe.UltimoLoginDiario = utilizador.UltimoLoginDiario;

                if (utilizador.TituloAtualId.HasValue)
                {
                    detalhe.TituloAtual = await _contexto.Titulos.AsNoTracking()
                        .Where(t => t.Id == utilizador.TituloAtualId.Value).Select(t => t.Nome).FirstOrDefaultAsync();
                }

                if (utilizador.MolduraPerfilAtualId.HasValue)
                {
                    detalhe.MolduraAtual = await _contexto.MoldurasPerfil.AsNoTracking()
                        .Where(m => m.Id == utilizador.MolduraPerfilAtualId.Value).Select(m => m.Nome).FirstOrDefaultAsync();
                }

                // ----- Recompensas: todas as que existem, marcando as obtidas -----
                detalhe.Recompensas = await ObterRecompensasAsync(utilizador);

                // ----- Últimas transações e últimas ações sobre a conta -----
                var transacoes = await _contexto.TransacoesMoeda.AsNoTracking()
                    .Where(t => t.UtilizadorId == id)
                    .OrderByDescending(t => t.DataCriacao).ThenByDescending(t => t.Id)
                    .Take(20)
                    .ToListAsync();

                detalhe.UltimasTransacoes = transacoes.Select(t => new AdminTransacao
                {
                    Id = t.Id,
                    MoedaNome = nomesMoeda.TryGetValue(t.TipoMoedaId, out var nomeMoeda) ? nomeMoeda : "?",
                    Montante = t.Montante,
                    TipoTransacao = t.TipoTransacao,
                    Origem = t.Origem,
                    Data = t.DataCriacao
                }).ToList();

                detalhe.UltimasAcoes = await ObterAcoesAsync(utilizadorAlvoId: id, adminId: null, limite: 20);

                return Ok(detalhe);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a conta: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/admin/acoes?adminId=1&utilizadorId=5&autorId=1&limite=100
        // ------------------------------------------------------------
        // Registo de ações. utilizadorId filtra pela conta alvo, autorId
        // pelo administrador que fez a ação; ambos opcionais.
        [HttpGet("acoes")]
        public async Task<ActionResult<IEnumerable<AdminAcaoResposta>>> ObterAcoes(
            [FromQuery] int adminId, [FromQuery] int? utilizadorId, [FromQuery] int? autorId, [FromQuery] int limite = 100)
        {
            try
            {
                var admin = await ObterAdminAsync(adminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem consultar o registo de ações.");
                }

                return Ok(await ObterAcoesAsync(utilizadorId, autorId, Math.Clamp(limite, 1, 500)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o registo de ações: " + ex.Message);
            }
        }

        // ============================================================
        //  ESTADO DA CONTA
        // ============================================================

        // ------------------------------------------------------------
        // POST: api/admin/utilizadores/5/estado
        // ------------------------------------------------------------
        [HttpPost("utilizadores/{id:int}/estado")]
        public async Task<ActionResult<AdminAcaoResultado>> AlterarEstado(int id, [FromBody] AdminEstadoPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem alterar contas.");
                }

                var utilizador = await _contexto.Utilizadores.FirstOrDefaultAsync(u => u.Id == id);
                if (utilizador == null)
                {
                    return NotFound("Conta não encontrada.");
                }

                if (!pedido.IsAtivo.HasValue && !pedido.IsAdmin.HasValue && !pedido.EmailValidado.HasValue)
                {
                    return BadRequest("Nada para alterar.");
                }

                // ----- Proteções -----
                bool vaiDesativar = pedido.IsAtivo == false && utilizador.IsAtivo;
                bool vaiDespromover = pedido.IsAdmin == false && utilizador.IsAdmin;

                if (utilizador.Id == admin.Id && (vaiDesativar || vaiDespromover))
                {
                    return BadRequest("Não podes desativar nem despromover a tua própria conta.");
                }

                if (utilizador.IsAdmin && (vaiDesativar || vaiDespromover))
                {
                    int outrosAdmins = await _contexto.Utilizadores
                        .CountAsync(u => u.IsAdmin && u.IsAtivo && u.Id != utilizador.Id);

                    if (outrosAdmins == 0)
                    {
                        return BadRequest("Esta é a única conta de administrador ativa — não pode ser desativada nem despromovida.");
                    }
                }

                // ----- Alterações -----
                var mudancas = new List<string>();

                if (pedido.IsAtivo.HasValue && pedido.IsAtivo.Value != utilizador.IsAtivo)
                {
                    utilizador.IsAtivo = pedido.IsAtivo.Value;
                    mudancas.Add(utilizador.IsAtivo ? "conta ativada" : "conta desativada");
                }

                if (pedido.IsAdmin.HasValue && pedido.IsAdmin.Value != utilizador.IsAdmin)
                {
                    utilizador.IsAdmin = pedido.IsAdmin.Value;
                    mudancas.Add(utilizador.IsAdmin ? "promovida a administrador" : "despromovida de administrador");
                }

                if (pedido.EmailValidado.HasValue && pedido.EmailValidado.Value != utilizador.EmailValidado)
                {
                    utilizador.EmailValidado = pedido.EmailValidado.Value;
                    mudancas.Add(utilizador.EmailValidado ? "email marcado como validado" : "validação de email retirada");
                }

                if (mudancas.Count == 0)
                {
                    return Ok(new AdminAcaoResultado { Mensagem = "A conta já estava nesse estado — nada foi alterado." });
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                string resumo = string.Join(", ", mudancas);

                RegistarAcao(admin.Id, utilizador.Id,
                    LogAdministrador.AcaoEstado + ": " + resumo,
                    "Utilizadores", utilizador.Id,
                    ComMotivo(Capitalizar(resumo) + ".", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = "Conta \"" + utilizador.NomeUtilizador + "\": " + resumo + "."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar o estado da conta: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/admin/utilizadores/5/repor-password
        // ------------------------------------------------------------
        // Define uma password nova sem precisar da antiga. Os tokens de
        // recuperação ainda válidos são invalidados. A password nunca vai
        // para o registo de ações.
        [HttpPost("utilizadores/{id:int}/repor-password")]
        public async Task<ActionResult<AdminAcaoResultado>> ReporPassword(int id, [FromBody] AdminReporPasswordPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem repor passwords.");
                }

                var utilizador = await _contexto.Utilizadores.FirstOrDefaultAsync(u => u.Id == id);
                if (utilizador == null)
                {
                    return NotFound("Conta não encontrada.");
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                bool tinhaPassword = !string.IsNullOrEmpty(utilizador.PasswordHash);
                utilizador.PasswordHash = PasswordHasher.GerarHash(pedido.NovaPassword);

                await _contexto.Database.ExecuteSqlAsync(
                    $@"UPDATE TokensRecuperacaoPassword SET Utilizado = 1
                       WHERE UtilizadorId = {utilizador.Id} AND Utilizado = 0 AND Token LIKE 'RP.%'");

                RegistarAcao(admin.Id, utilizador.Id,
                    LogAdministrador.AcaoPassword + ": " + (tinhaPassword ? "reposta" : "definida"),
                    "Utilizadores", utilizador.Id,
                    ComMotivo(tinhaPassword
                        ? "Password reposta pelo administrador; tokens de recuperação invalidados."
                        : "Primeira password local definida pelo administrador (conta Google).", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = "Password de \"" + utilizador.NomeUtilizador + "\" " + (tinhaPassword ? "reposta" : "definida") + " com sucesso."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao repor a password: " + ex.Message);
            }
        }

        // ============================================================
        //  MOEDA
        // ============================================================

        // ------------------------------------------------------------
        // POST: api/admin/utilizadores/5/moeda
        // ------------------------------------------------------------
        // Quantidade > 0 credita; < 0 debita (nunca abaixo de zero).
        [HttpPost("utilizadores/{id:int}/moeda")]
        public async Task<ActionResult<AdminAcaoResultado>> AjustarMoeda(int id, [FromBody] AdminMoedaPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem alterar saldos.");
                }

                var utilizador = await _contexto.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (utilizador == null)
                {
                    return NotFound("Conta não encontrada.");
                }

                if (pedido.Quantidade == 0)
                {
                    return BadRequest("Indica uma quantidade diferente de zero (positiva para dar, negativa para tirar).");
                }

                var tipoMoeda = await _contexto.TiposMoeda.AsNoTracking().FirstOrDefaultAsync(t => t.Id == pedido.TipoMoedaId);
                if (tipoMoeda == null)
                {
                    return BadRequest("Tipo de moeda inválido.");
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                // Carteira em falta (contas criadas por script) é criada a zero
                await _contexto.Database.ExecuteSqlAsync(
                    $@"INSERT INTO CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
                       SELECT {id}, {tipoMoeda.Id}, 0
                       WHERE NOT EXISTS (SELECT 1 FROM CarteirasUtilizador
                                         WHERE UtilizadorId = {id} AND TipoMoedaId = {tipoMoeda.Id})");

                decimal movimento;
                string tipoTransacao;

                if (pedido.Quantidade > 0)
                {
                    movimento = decimal.Round(pedido.Quantidade, 2);
                    tipoTransacao = TransacaoMoeda.TipoGanho;

                    await _contexto.Database.ExecuteSqlAsync(
                        $@"UPDATE CarteirasUtilizador SET Saldo = Saldo + {movimento}
                           WHERE UtilizadorId = {id} AND TipoMoedaId = {tipoMoeda.Id}");
                }
                else
                {
                    // Débito: tira no máximo o que houver. O UPDATE condicional
                    // garante que o saldo não fica negativo mesmo com dois
                    // pedidos ao mesmo tempo.
                    decimal pedida = decimal.Round(Math.Abs(pedido.Quantidade), 2);
                    decimal saldoAtual = await _contexto.Carteiras.AsNoTracking()
                        .Where(c => c.UtilizadorId == id && c.TipoMoedaId == tipoMoeda.Id)
                        .Select(c => c.Saldo)
                        .FirstOrDefaultAsync();

                    movimento = Math.Min(pedida, saldoAtual);
                    tipoTransacao = TransacaoMoeda.TipoGasto;

                    if (movimento > 0)
                    {
                        int alteradas = await _contexto.Database.ExecuteSqlAsync(
                            $@"UPDATE CarteirasUtilizador SET Saldo = Saldo - {movimento}
                               WHERE UtilizadorId = {id} AND TipoMoedaId = {tipoMoeda.Id} AND Saldo >= {movimento}");

                        if (alteradas == 0)
                        {
                            return Conflict("O saldo mudou entretanto — volta a tentar.");
                        }
                    }
                }

                if (movimento > 0)
                {
                    _contexto.TransacoesMoeda.Add(new TransacaoMoeda
                    {
                        UtilizadorId = id,
                        TipoMoedaId = tipoMoeda.Id,
                        Montante = movimento,
                        TipoTransacao = tipoTransacao,
                        Origem = OrigemAdmin(pedido.Motivo),
                        DataCriacao = DateTime.UtcNow
                    });
                }

                await _contexto.SaveChangesAsync();

                decimal novoSaldo = await _contexto.Carteiras.AsNoTracking()
                    .Where(c => c.UtilizadorId == id && c.TipoMoedaId == tipoMoeda.Id)
                    .Select(c => c.Saldo)
                    .FirstOrDefaultAsync();

                string sinal = tipoTransacao == TransacaoMoeda.TipoGanho ? "+" : "-";
                string resumo = sinal + movimento.ToString("0.##") + " " + tipoMoeda.Nome;

                RegistarAcao(admin.Id, id,
                    LogAdministrador.AcaoMoeda + ": " + resumo,
                    "CarteirasUtilizador", tipoMoeda.Id,
                    ComMotivo(resumo + " (saldo: " + novoSaldo.ToString("0.##") + ").", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                string mensagem = movimento == 0
                    ? "A conta não tinha " + tipoMoeda.Nome + " para retirar (saldo 0)."
                    : (tipoTransacao == TransacaoMoeda.TipoGanho ? "Dados " : "Retirados ") + movimento.ToString("0.##") + " " + tipoMoeda.Nome +
                      " a \"" + utilizador.NomeUtilizador + "\". Novo saldo: " + novoSaldo.ToString("0.##") + ".";

                if (tipoTransacao == TransacaoMoeda.TipoGasto && movimento > 0 && movimento < Math.Abs(pedido.Quantidade))
                {
                    mensagem += " (Só havia " + movimento.ToString("0.##") + " — o saldo não fica negativo.)";
                }

                return Ok(new AdminAcaoResultado { Mensagem = mensagem, NovoValor = novoSaldo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar o saldo: " + ex.Message);
            }
        }

        // ============================================================
        //  PERSONAGENS E INVENTÁRIO
        // ============================================================

        // ------------------------------------------------------------
        // POST: api/admin/utilizadores/5/personagem
        // ------------------------------------------------------------
        // Quantidade > 0 dá cópias (respeita o espaço do inventário);
        // < 0 tira cópias (a zero, a linha da coleção é apagada).
        [HttpPost("utilizadores/{id:int}/personagem")]
        public async Task<ActionResult<AdminAcaoResultado>> AjustarPersonagem(int id, [FromBody] AdminPersonagemPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem alterar coleções.");
                }

                var utilizador = await _contexto.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (utilizador == null)
                {
                    return NotFound("Conta não encontrada.");
                }

                if (pedido.Quantidade == 0)
                {
                    return BadRequest("Indica uma quantidade diferente de zero (positiva para dar, negativa para tirar).");
                }

                var personagem = await _contexto.Personagens.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pedido.PersonagemId);
                if (personagem == null)
                {
                    return BadRequest("Personagem inválida.");
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                string mensagem;
                int novaQuantidade;

                if (pedido.Quantidade > 0)
                {
                    // ----- Dar cópias: primeiro o espaço -----
                    await GarantirInventarioAsync(id);

                    var inventario = await _contexto.Inventarios.AsNoTracking().FirstAsync(i => i.UtilizadorId == id);
                    int ocupado = await _contexto.Colecoes.Where(c => c.UtilizadorId == id).SumAsync(c => (int?)c.Quantidade) ?? 0;

                    if (ocupado + pedido.Quantidade > inventario.CapacidadeTotal)
                    {
                        int livres = Math.Max(0, inventario.CapacidadeTotal - ocupado);
                        return BadRequest("A coleção de \"" + utilizador.NomeUtilizador + "\" não tem espaço: " + livres +
                                          " lugar(es) livre(s) de " + inventario.CapacidadeTotal +
                                          ". Aumenta a capacidade extra do inventário primeiro.");
                    }

                    // UPDATE se já tem a personagem; senão INSERT da primeira cópia
                    // (nível de amizade 1 = Desconhecido, como numa invocação).
                    int atualizadas = await _contexto.Database.ExecuteSqlAsync(
                        $@"UPDATE ColecaoUtilizador SET Quantidade = Quantidade + {pedido.Quantidade}
                           WHERE UtilizadorId = {id} AND PersonagemId = {personagem.Id}");

                    if (atualizadas == 0)
                    {
                        await _contexto.Database.ExecuteSqlAsync(
                            $@"INSERT INTO ColecaoUtilizador (UtilizadorId, PersonagemId, Quantidade, IsFavorito, PontosAmizade, NivelAmizadeId, DataObtencao)
                               VALUES ({id}, {personagem.Id}, {pedido.Quantidade}, 0, 0, 1, {DateTime.UtcNow})");
                    }

                    novaQuantidade = await QuantidadeAsync(id, personagem.Id);
                    mensagem = (atualizadas == 0 ? "\"" + personagem.Nome + "\" acrescentada à coleção de \"" : "Mais " + pedido.Quantidade + " cópia(s) de \"" + personagem.Nome + "\" para \"") +
                               utilizador.NomeUtilizador + "\" (agora x" + novaQuantidade + ").";
                }
                else
                {
                    // ----- Tirar cópias -----
                    var item = await _contexto.Colecoes.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.UtilizadorId == id && c.PersonagemId == personagem.Id);

                    if (item == null)
                    {
                        return BadRequest("\"" + utilizador.NomeUtilizador + "\" não tem a personagem \"" + personagem.Nome + "\".");
                    }

                    int pedidas = Math.Abs(pedido.Quantidade);
                    int retirar = Math.Min(pedidas, item.Quantidade);

                    if (retirar >= item.Quantidade)
                    {
                        // Última cópia: a linha sai (e com ela os pontos de amizade).
                        // Se o utilizador tinha o título/moldura dessa personagem
                        // equipados, ficam — as recompensas já ganhas não se perdem.
                        await _contexto.Database.ExecuteSqlAsync(
                            $@"DELETE FROM ColecaoUtilizador WHERE UtilizadorId = {id} AND PersonagemId = {personagem.Id}");

                        novaQuantidade = 0;
                        mensagem = "\"" + personagem.Nome + "\" retirada por completo da coleção de \"" + utilizador.NomeUtilizador +
                                   "\" (" + item.Quantidade + " cópia(s); a amizade com ela foi a zero).";
                    }
                    else
                    {
                        await _contexto.Database.ExecuteSqlAsync(
                            $@"UPDATE ColecaoUtilizador SET Quantidade = Quantidade - {retirar}
                               WHERE UtilizadorId = {id} AND PersonagemId = {personagem.Id} AND Quantidade > {retirar}");

                        novaQuantidade = await QuantidadeAsync(id, personagem.Id);
                        mensagem = "Retiradas " + retirar + " cópia(s) de \"" + personagem.Nome + "\" a \"" + utilizador.NomeUtilizador +
                                   "\" (agora x" + novaQuantidade + ").";
                    }
                }

                string resumo = (pedido.Quantidade > 0 ? "+" : "") + pedido.Quantidade + " " + personagem.Nome;

                RegistarAcao(admin.Id, id,
                    LogAdministrador.AcaoPersonagem + ": " + resumo,
                    "ColecaoUtilizador", personagem.Id,
                    ComMotivo(resumo + " (agora x" + novaQuantidade + ").", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado { Mensagem = mensagem, NovoValor = novaQuantidade });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar a coleção: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/admin/utilizadores/5/inventario
        // ------------------------------------------------------------
        [HttpPost("utilizadores/{id:int}/inventario")]
        public async Task<ActionResult<AdminAcaoResultado>> DefinirInventario(int id, [FromBody] AdminInventarioPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem alterar inventários.");
                }

                var utilizador = await _contexto.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (utilizador == null)
                {
                    return NotFound("Conta não encontrada.");
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                await GarantirInventarioAsync(id);

                int anterior = await _contexto.Inventarios.AsNoTracking()
                    .Where(i => i.UtilizadorId == id).Select(i => i.CapacidadeExtra).FirstAsync();

                await _contexto.Database.ExecuteSqlAsync(
                    $@"UPDATE InventarioUtilizador SET CapacidadeExtra = {pedido.CapacidadeExtra} WHERE UtilizadorId = {id}");

                int capacidadeBase = await _contexto.Inventarios.AsNoTracking()
                    .Where(i => i.UtilizadorId == id).Select(i => i.CapacidadeBase).FirstAsync();
                int total = capacidadeBase + pedido.CapacidadeExtra;

                RegistarAcao(admin.Id, id,
                    LogAdministrador.AcaoInventario + ": extra " + anterior + " → " + pedido.CapacidadeExtra,
                    "InventarioUtilizador", id,
                    ComMotivo("Capacidade extra " + anterior + " → " + pedido.CapacidadeExtra + " (total " + total + ").", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = "Inventário de \"" + utilizador.NomeUtilizador + "\": capacidade extra " + pedido.CapacidadeExtra +
                               " (total " + total + " lugares).",
                    NovoValor = total
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar o inventário: " + ex.Message);
            }
        }

        // ============================================================
        //  AMIZADE E RECOMPENSAS
        // ============================================================

        // ------------------------------------------------------------
        // POST: api/admin/utilizadores/5/amizade
        // ------------------------------------------------------------
        // Define os pontos com uma personagem da coleção. Passa pelo
        // ServicoAmizade, por isso o limite da raridade, o recálculo do
        // nível e as recompensas por subida são exatamente os do jogo.
        [HttpPost("utilizadores/{id:int}/amizade")]
        public async Task<ActionResult<AdminAcaoResultado>> DefinirAmizade(int id, [FromBody] AdminAmizadePedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem alterar a amizade.");
                }

                var utilizador = await _contexto.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (utilizador == null)
                {
                    return NotFound("Conta não encontrada.");
                }

                var item = await _contexto.Colecoes.AsNoTracking()
                    .Include(c => c.Personagem)
                    .FirstOrDefaultAsync(c => c.UtilizadorId == id && c.PersonagemId == pedido.PersonagemId);

                if (item == null)
                {
                    return BadRequest("A conta não tem essa personagem na coleção — dá-lhe primeiro a personagem.");
                }

                string nomePersonagem = item.Personagem?.Nome ?? "?";
                int antes = item.PontosAmizade;

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                // Somar a diferença (pode ser negativa): o serviço limita ao
                // máximo da raridade e recalcula o nível para cima OU para baixo.
                var resultado = await _amizade.AdicionarPontosAsync(id, pedido.PersonagemId, pedido.Pontos - antes, marcarInteracao: false);
                if (resultado == null)
                {
                    return BadRequest("A conta não tem essa personagem na coleção.");
                }

                var recompensas = new List<RecompensaAmizade>();
                if (resultado.SubiuDeNivel)
                {
                    recompensas = await _amizade.DesbloquearRecompensasAsync(
                        id, pedido.PersonagemId, nomePersonagem, resultado.NivelAnterior, resultado.NivelAtual);
                }

                string resumo = nomePersonagem + " " + antes + " → " + resultado.PontosAmizade + " (" + resultado.NivelAtual.Nome + ")";

                string detalhes = "Pontos " + resumo + ".";
                if (recompensas.Count > 0)
                {
                    detalhes += " Desbloqueado: " + string.Join(", ", recompensas.Select(ServicoAmizade.DescreverRecompensa)) + ".";
                }

                RegistarAcao(admin.Id, id,
                    LogAdministrador.AcaoAmizade + ": " + resumo,
                    "ColecaoUtilizador", pedido.PersonagemId,
                    ComMotivo(detalhes, pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                string mensagem = "Amizade de \"" + utilizador.NomeUtilizador + "\" com " + nomePersonagem + ": " +
                                  resultado.PontosAmizade + " pontos, nível \"" + resultado.NivelAtual.Nome + "\".";

                if (resultado.PontosAmizade != pedido.Pontos)
                {
                    mensagem += " (Limitado ao máximo da raridade.)";
                }

                if (recompensas.Count > 0)
                {
                    mensagem += " Desbloqueado: " + string.Join(", ", recompensas.Select(ServicoAmizade.DescreverRecompensa)) + ".";
                }

                return Ok(new AdminAcaoResultado { Mensagem = mensagem, NovoValor = resultado.PontosAmizade });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar a amizade: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/admin/utilizadores/5/recompensa
        // ------------------------------------------------------------
        // Conceder = true dá o título/emblema/moldura; false tira-o (e
        // desequipa-o do perfil se estiver em uso).
        [HttpPost("utilizadores/{id:int}/recompensa")]
        public async Task<ActionResult<AdminAcaoResultado>> AjustarRecompensa(int id, [FromBody] AdminRecompensaPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem alterar recompensas.");
                }

                var utilizador = await _contexto.Utilizadores.FirstOrDefaultAsync(u => u.Id == id);
                if (utilizador == null)
                {
                    return NotFound("Conta não encontrada.");
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                string nome;
                string tabela;
                bool mudou;

                switch (pedido.Tipo)
                {
                    case "Titulo":
                    {
                        var titulo = await _contexto.Titulos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == pedido.Id);
                        if (titulo == null)
                        {
                            return BadRequest("Título inválido.");
                        }

                        nome = titulo.Nome;
                        tabela = "TitulosUtilizador";

                        if (pedido.Conceder)
                        {
                            mudou = await _contexto.Database.ExecuteSqlAsync(
                                $@"INSERT INTO TitulosUtilizador (UtilizadorId, TituloId, DataObtencao)
                                   SELECT {id}, {titulo.Id}, SYSUTCDATETIME()
                                   WHERE NOT EXISTS (SELECT 1 FROM TitulosUtilizador WHERE UtilizadorId = {id} AND TituloId = {titulo.Id})") == 1;
                        }
                        else
                        {
                            mudou = await _contexto.Database.ExecuteSqlAsync(
                                $@"DELETE FROM TitulosUtilizador WHERE UtilizadorId = {id} AND TituloId = {titulo.Id}") == 1;

                            if (utilizador.TituloAtualId == titulo.Id)
                            {
                                utilizador.TituloAtualId = null;
                            }
                        }
                        break;
                    }

                    case "Emblema":
                    {
                        var emblema = await _contexto.Emblemas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == pedido.Id);
                        if (emblema == null)
                        {
                            return BadRequest("Emblema inválido.");
                        }

                        nome = emblema.Nome;
                        tabela = "EmblemasUtilizador";

                        if (pedido.Conceder)
                        {
                            mudou = await _contexto.Database.ExecuteSqlAsync(
                                $@"INSERT INTO EmblemasUtilizador (UtilizadorId, EmblemaId, DataObtencao, IsEquipado)
                                   SELECT {id}, {emblema.Id}, SYSUTCDATETIME(), 0
                                   WHERE NOT EXISTS (SELECT 1 FROM EmblemasUtilizador WHERE UtilizadorId = {id} AND EmblemaId = {emblema.Id})") == 1;
                        }
                        else
                        {
                            // Apagar a linha tira-o também do perfil (IsEquipado vive nela)
                            mudou = await _contexto.Database.ExecuteSqlAsync(
                                $@"DELETE FROM EmblemasUtilizador WHERE UtilizadorId = {id} AND EmblemaId = {emblema.Id}") == 1;
                        }
                        break;
                    }

                    case "Moldura":
                    {
                        var moldura = await _contexto.MoldurasPerfil.AsNoTracking().FirstOrDefaultAsync(m => m.Id == pedido.Id);
                        if (moldura == null)
                        {
                            return BadRequest("Moldura inválida.");
                        }

                        nome = moldura.Nome;
                        tabela = "MoldurasUtilizador";

                        if (pedido.Conceder)
                        {
                            mudou = await _contexto.Database.ExecuteSqlAsync(
                                $@"INSERT INTO MoldurasUtilizador (UtilizadorId, MolduraId, DataObtencao)
                                   SELECT {id}, {moldura.Id}, SYSUTCDATETIME()
                                   WHERE NOT EXISTS (SELECT 1 FROM MoldurasUtilizador WHERE UtilizadorId = {id} AND MolduraId = {moldura.Id})") == 1;
                        }
                        else
                        {
                            mudou = await _contexto.Database.ExecuteSqlAsync(
                                $@"DELETE FROM MoldurasUtilizador WHERE UtilizadorId = {id} AND MolduraId = {moldura.Id}") == 1;

                            if (utilizador.MolduraPerfilAtualId == moldura.Id)
                            {
                                utilizador.MolduraPerfilAtualId = null;
                            }
                        }
                        break;
                    }

                    default:
                        return BadRequest("Tipo de recompensa inválido.");
                }

                if (!mudou)
                {
                    return Ok(new AdminAcaoResultado
                    {
                        Mensagem = pedido.Conceder
                            ? "\"" + utilizador.NomeUtilizador + "\" já tinha " + DescreverTipo(pedido.Tipo) + " \"" + nome + "\"."
                            : "\"" + utilizador.NomeUtilizador + "\" não tinha " + DescreverTipo(pedido.Tipo) + " \"" + nome + "\"."
                    });
                }

                string resumo = (pedido.Conceder ? "+" : "-") + DescreverTipo(pedido.Tipo) + " " + nome;

                RegistarAcao(admin.Id, id,
                    LogAdministrador.AcaoRecompensa + ": " + resumo,
                    tabela, pedido.Id,
                    ComMotivo((pedido.Conceder ? "Concedido " : "Revogado ") + DescreverTipo(pedido.Tipo) + " \"" + nome + "\".", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = Capitalizar(DescreverTipo(pedido.Tipo)) + " \"" + nome + "\" " +
                               (pedido.Conceder ? "concedido a" : "retirado a") + " \"" + utilizador.NomeUtilizador + "\"."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar a recompensa: " + ex.Message);
            }
        }

        // ============================================================
        //  Métodos auxiliares
        // ============================================================

        /// <summary>Conta de administrador ativa com este Id, ou null.</summary>
        private async Task<Utilizador?> ObterAdminAsync(int adminId)
        {
            if (adminId <= 0)
            {
                return null;
            }

            return await _contexto.Utilizadores.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == adminId && u.IsAdmin && u.IsAtivo);
        }

        /// <summary>
        /// Acrescenta a linha do registo de ações ao contexto. Quem chama faz
        /// o SaveChanges dentro da sua transação, para o registo ficar (ou
        /// cair) junto com a ação.
        /// </summary>
        private void RegistarAcao(int adminId, int? alvoId, string acao, string? tabela, int? registoId, string? detalhes)
        {
            _contexto.LogsAdministrador.Add(new LogAdministrador
            {
                AdminId = adminId,
                UtilizadorAlvoId = alvoId,
                Acao = Truncar(acao, 100),
                TabelaAlvo = tabela == null ? null : Truncar(tabela, 50),
                RegistoAlvoId = registoId,
                Detalhes = detalhes == null ? null : Truncar(detalhes, 500),
                DataCriacao = DateTime.UtcNow
            });
        }

        private async Task<List<AdminAcaoResposta>> ObterAcoesAsync(int? utilizadorAlvoId, int? adminId, int limite)
        {
            IQueryable<LogAdministrador> consulta = _contexto.LogsAdministrador.AsNoTracking();

            if (utilizadorAlvoId.HasValue)
            {
                consulta = consulta.Where(l => l.UtilizadorAlvoId == utilizadorAlvoId.Value);
            }

            if (adminId.HasValue)
            {
                consulta = consulta.Where(l => l.AdminId == adminId.Value);
            }

            var logs = await consulta
                .OrderByDescending(l => l.DataCriacao).ThenByDescending(l => l.Id)
                .Take(limite)
                .ToListAsync();

            var ids = logs.Select(l => l.AdminId)
                .Concat(logs.Where(l => l.UtilizadorAlvoId.HasValue).Select(l => l.UtilizadorAlvoId!.Value))
                .Distinct()
                .ToList();

            var nomes = await _contexto.Utilizadores.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.NomeUtilizador);

            return logs.Select(l => new AdminAcaoResposta
            {
                Id = l.Id,
                AdminId = l.AdminId,
                AdminNome = nomes.TryGetValue(l.AdminId, out var a) ? a : "#" + l.AdminId,
                UtilizadorAlvoId = l.UtilizadorAlvoId,
                UtilizadorAlvoNome = l.UtilizadorAlvoId.HasValue
                    ? (nomes.TryGetValue(l.UtilizadorAlvoId.Value, out var n) ? n : "#" + l.UtilizadorAlvoId)
                    : null,
                Acao = l.Acao,
                TabelaAlvo = l.TabelaAlvo,
                RegistoAlvoId = l.RegistoAlvoId,
                Detalhes = l.Detalhes,
                Data = l.DataCriacao
            }).ToList();
        }

        /// <summary>Todas as recompensas de amizade que existem, marcando as que a conta já tem.</summary>
        private async Task<List<AdminRecompensa>> ObterRecompensasAsync(Utilizador utilizador)
        {
            int id = utilizador.Id;

            var nomesPersonagens = await _contexto.Personagens.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nome);
            var niveis = await _contexto.NiveisAmizade.AsNoTracking().ToDictionaryAsync(n => n.Id, n => n);

            string? NomePersonagem(int? pid) => pid.HasValue && nomesPersonagens.TryGetValue(pid.Value, out var n) ? n : null;
            string? NomeNivel(int? nid) => nid.HasValue && niveis.TryGetValue(nid.Value, out var n) ? n.Nome : null;
            int OrdemNivel(int? nid) => nid.HasValue && niveis.TryGetValue(nid.Value, out var n) ? n.Ordem : 0;

            var lista = new List<AdminRecompensa>();

            var titulosGanhos = await _contexto.TitulosUtilizador.AsNoTracking()
                .Where(t => t.UtilizadorId == id).ToDictionaryAsync(t => t.TituloId, t => t.DataObtencao);

            foreach (var t in await _contexto.Titulos.AsNoTracking().ToListAsync())
            {
                lista.Add(new AdminRecompensa
                {
                    Tipo = "Titulo",
                    Id = t.Id,
                    Nome = t.Nome,
                    PersonagemNome = NomePersonagem(t.PersonagemId),
                    NivelNome = NomeNivel(t.NivelAmizadeId),
                    NivelOrdem = OrdemNivel(t.NivelAmizadeId),
                    CorHex = t.CorHex,
                    Obtida = titulosGanhos.ContainsKey(t.Id),
                    Equipada = utilizador.TituloAtualId == t.Id,
                    DataObtencao = titulosGanhos.TryGetValue(t.Id, out var d1) ? d1 : null
                });
            }

            var emblemasGanhos = await _contexto.EmblemasUtilizador.AsNoTracking()
                .Where(e => e.UtilizadorId == id).ToDictionaryAsync(e => e.EmblemaId, e => e);

            foreach (var e in await _contexto.Emblemas.AsNoTracking().ToListAsync())
            {
                emblemasGanhos.TryGetValue(e.Id, out var ganho);

                lista.Add(new AdminRecompensa
                {
                    Tipo = "Emblema",
                    Id = e.Id,
                    Nome = e.Nome,
                    PersonagemNome = NomePersonagem(e.PersonagemId),
                    NivelNome = NomeNivel(e.NivelAmizadeId),
                    NivelOrdem = OrdemNivel(e.NivelAmizadeId),
                    Obtida = ganho != null,
                    Equipada = ganho?.IsEquipado ?? false,
                    DataObtencao = ganho?.DataObtencao
                });
            }

            var moldurasGanhas = await _contexto.MoldurasUtilizador.AsNoTracking()
                .Where(m => m.UtilizadorId == id).ToDictionaryAsync(m => m.MolduraId, m => m.DataObtencao);

            foreach (var m in await _contexto.MoldurasPerfil.AsNoTracking().ToListAsync())
            {
                lista.Add(new AdminRecompensa
                {
                    Tipo = "Moldura",
                    Id = m.Id,
                    Nome = m.Nome,
                    PersonagemNome = NomePersonagem(m.PersonagemId),
                    NivelNome = NomeNivel(m.NivelAmizadeId),
                    NivelOrdem = OrdemNivel(m.NivelAmizadeId),
                    CorHex = m.CorHex,
                    Obtida = moldurasGanhas.ContainsKey(m.Id),
                    Equipada = utilizador.MolduraPerfilAtualId == m.Id,
                    DataObtencao = moldurasGanhas.TryGetValue(m.Id, out var d3) ? d3 : null
                });
            }

            // Ordem: por personagem, depois pelo nível da recompensa, depois tipo
            var ordemTipo = new Dictionary<string, int> { ["Titulo"] = 1, ["Emblema"] = 2, ["Moldura"] = 3 };

            return lista
                .OrderBy(r => r.PersonagemNome ?? "\uFFFF")
                .ThenBy(r => r.NivelOrdem)
                .ThenBy(r => ordemTipo[r.Tipo])
                .ToList();
        }

        private async Task GarantirInventarioAsync(int utilizadorId)
        {
            await _contexto.Database.ExecuteSqlAsync(
                $@"INSERT INTO InventarioUtilizador (UtilizadorId, CapacidadeBase, CapacidadeExtra)
                   SELECT {utilizadorId}, {CapacidadeBasePadrao}, 0
                   WHERE NOT EXISTS (SELECT 1 FROM InventarioUtilizador WHERE UtilizadorId = {utilizadorId})");
        }

        private async Task<int> QuantidadeAsync(int utilizadorId, int personagemId)
        {
            return await _contexto.Colecoes.AsNoTracking()
                .Where(c => c.UtilizadorId == utilizadorId && c.PersonagemId == personagemId)
                .Select(c => c.Quantidade)
                .FirstOrDefaultAsync();
        }

        private static AdminUtilizadorResumo ParaResumo(Utilizador u) => new AdminUtilizadorResumo
        {
            Id = u.Id,
            NomeUtilizador = u.NomeUtilizador,
            Email = u.Email,
            EmailValidado = u.EmailValidado,
            IsAdmin = u.IsAdmin,
            IsAtivo = u.IsAtivo,
            TemPasswordLocal = !string.IsNullOrEmpty(u.PasswordHash),
            ContaGoogle = !string.IsNullOrEmpty(u.GoogleId),
            FotoPerfilUrl = u.FotoPerfilUrl,
            DataCriacao = u.DataCriacao,
            UltimoLogin = u.UltimoLogin
        };

        /// <summary>"Admin: motivo" com 50 caracteres no máximo (limite da coluna Origem).</summary>
        private static string OrigemAdmin(string? motivo)
        {
            string origem = "Admin";
            if (!string.IsNullOrWhiteSpace(motivo))
            {
                origem += ": " + motivo.Trim();
            }
            return Truncar(origem, 50);
        }

        private static string ComMotivo(string detalhes, string? motivo) =>
            string.IsNullOrWhiteSpace(motivo) ? detalhes : detalhes + " Motivo: " + motivo.Trim();

        private static string DescreverTipo(string tipo) => tipo switch
        {
            "Emblema" => "o emblema",
            "Moldura" => "a moldura",
            _ => "o título"
        };

        private static string Capitalizar(string texto) =>
            string.IsNullOrEmpty(texto) ? texto : char.ToUpperInvariant(texto[0]) + texto.Substring(1);

        private static string Truncar(string texto, int max) =>
            texto.Length <= max ? texto : texto.Substring(0, max - 1) + "…";
    }
}
