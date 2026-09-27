using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// GESTÃO DAS MOEDAS (api/admin/moedas) — a economia vista de cima.
    /// (Dar/tirar moeda a UMA conta continua em api/admin/utilizadores/{id}/moeda.)
    ///
    ///   GET    api/admin/moedas?adminId=              - tipos de moeda com o total em
    ///                                                   circulação, ganhos/gastos dos
    ///                                                   últimos 30 dias e origens
    ///   POST   api/admin/moedas/tipos                 - criar um tipo de moeda (cria a
    ///                                                   carteira a zero em todas as contas)
    ///   PUT    api/admin/moedas/tipos/{id}            - mudar o nome
    ///   DELETE api/admin/moedas/tipos/{id}?adminId=   - apagar (nunca Gemas/Moedas/Bilhetes;
    ///                                                   só sem movimentos nem saldos)
    ///   POST   api/admin/moedas/oferta                - dá uma quantia a TODAS as contas
    ///                                                   ativas (compensação, evento, ...)
    ///
    /// A oferta faz tudo em SQL, numa transação: soma o saldo, grava uma
    /// linha em TransacoesMoeda por conta (Ganho, origem "Admin: oferta…")
    /// e, se pedido, uma notificação "Recompensa". Fica UMA linha no registo
    /// de ações com o total.
    /// </summary>
    [Route("api/admin/moedas")]
    [ApiController]
    public class AdminMoedasController : AdminBaseController
    {
        private const int DiasResumo = 30;
        private const decimal OfertaMaxima = 100000m;

        public AdminMoedasController(WishBoundContext contexto) : base(contexto)
        {
        }

        // GET: api/admin/moedas?adminId=1
        [HttpGet]
        public async Task<ActionResult<AdminMoedasResposta>> Obter([FromQuery] int adminId)
        {
            try
            {
                if (await ObterAdminAsync(adminId) == null)
                {
                    return ApenasAdmins("gerir as moedas");
                }

                var desde = DateTime.UtcNow.AddDays(-DiasResumo);
                var tipos = await _contexto.TiposMoeda.AsNoTracking().OrderBy(t => t.Id).ToListAsync();

                var saldos = await _contexto.Carteiras.AsNoTracking()
                    .GroupBy(c => c.TipoMoedaId)
                    .Select(g => new { TipoMoedaId = g.Key, Total = g.Sum(c => c.Saldo), Contas = g.Count(c => c.Saldo > 0) })
                    .ToListAsync();

                var contagens = await _contexto.TransacoesMoeda.AsNoTracking()
                    .GroupBy(t => t.TipoMoedaId)
                    .Select(g => new { TipoMoedaId = g.Key, Total = g.Count() })
                    .ToListAsync();

                var recentes = await _contexto.TransacoesMoeda.AsNoTracking()
                    .Where(t => t.DataCriacao >= desde)
                    .GroupBy(t => new { t.TipoMoedaId, t.TipoTransacao })
                    .Select(g => new { g.Key.TipoMoedaId, g.Key.TipoTransacao, Total = g.Sum(t => t.Montante) })
                    .ToListAsync();

                var resposta = new AdminMoedasResposta
                {
                    ContasAtivas = await _contexto.Utilizadores.CountAsync(u => u.IsAtivo && u.NomeUtilizador != "Sistema")
                };

                foreach (var t in tipos)
                {
                    var s = saldos.FirstOrDefault(x => x.TipoMoedaId == t.Id);
                    int nTransacoes = contagens.FirstOrDefault(c => c.TipoMoedaId == t.Id)?.Total ?? 0;
                    bool protegida = EhProtegida(t.Id);

                    resposta.Tipos.Add(new AdminTipoMoeda
                    {
                        Id = t.Id,
                        Nome = t.Nome,
                        EmCirculacao = s?.Total ?? 0m,
                        ContasComSaldo = s?.Contas ?? 0,
                        Ganho30Dias = recentes.Where(m => m.TipoMoedaId == t.Id && m.TipoTransacao == TransacaoMoeda.TipoGanho).Sum(m => m.Total),
                        Gasto30Dias = recentes.Where(m => m.TipoMoedaId == t.Id && m.TipoTransacao == TransacaoMoeda.TipoGasto).Sum(m => m.Total),
                        Transacoes = nTransacoes,
                        Protegida = protegida,
                        PodeApagar = !protegida && nTransacoes == 0 && (s?.Total ?? 0m) == 0m
                    });
                }

                // Origens mais comuns do dinheiro que entra e sai (últimos 30 dias)
                var nomes = tipos.ToDictionary(t => t.Id, t => t.Nome);
                var origens = await _contexto.TransacoesMoeda.AsNoTracking()
                    .Where(t => t.DataCriacao >= desde)
                    .GroupBy(t => new { t.TipoMoedaId, t.TipoTransacao, t.Origem })
                    .Select(g => new { g.Key.TipoMoedaId, g.Key.TipoTransacao, g.Key.Origem, Total = g.Sum(t => t.Montante), Movimentos = g.Count() })
                    .ToListAsync();

                resposta.OrigensGanho = origens
                    .Where(o => o.TipoTransacao == TransacaoMoeda.TipoGanho)
                    .OrderByDescending(o => o.Total).Take(10)
                    .Select(o => new AdminOrigemMoeda { Moeda = nomes.GetValueOrDefault(o.TipoMoedaId, "?"), Origem = o.Origem, Total = o.Total, Movimentos = o.Movimentos })
                    .ToList();

                resposta.OrigensGasto = origens
                    .Where(o => o.TipoTransacao == TransacaoMoeda.TipoGasto)
                    .OrderByDescending(o => o.Total).Take(10)
                    .Select(o => new AdminOrigemMoeda { Moeda = nomes.GetValueOrDefault(o.TipoMoedaId, "?"), Origem = o.Origem, Total = o.Total, Movimentos = o.Movimentos })
                    .ToList();

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a economia: " + ex.Message);
            }
        }

        // POST: api/admin/moedas/tipos
        [HttpPost("tipos")]
        public async Task<ActionResult<AdminAcaoResultado>> CriarTipo([FromBody] AdminTipoMoedaPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir as moedas");
                }

                string? erro = await ValidarNomeAsync(pedido.Nome, idAtual: null);
                if (erro != null)
                {
                    return BadRequest(erro);
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                var tipo = new TipoMoeda { Nome = pedido.Nome.Trim() };
                _contexto.TiposMoeda.Add(tipo);
                await _contexto.SaveChangesAsync();

                // Carteira a zero em todas as contas (o registo de contas novas já cria uma por tipo)
                int carteiras = await _contexto.Database.ExecuteSqlAsync(
                    $@"INSERT INTO CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
                       SELECT u.UtilizadorId, {tipo.Id}, 0 FROM Utilizadores u
                       WHERE NOT EXISTS (SELECT 1 FROM CarteirasUtilizador c
                                         WHERE c.UtilizadorId = u.UtilizadorId AND c.TipoMoedaId = {tipo.Id})");

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoMoeda + ": tipo " + tipo.Nome + " criado",
                    "TiposMoeda", tipo.Id,
                    ComMotivo(carteiras + " carteiras criadas a zero.", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Moeda \"" + tipo.Nome + "\" criada (" + carteiras + " carteiras a zero)." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao criar a moeda: " + ex.Message);
            }
        }

        // PUT: api/admin/moedas/tipos/4
        [HttpPut("tipos/{id:int}")]
        public async Task<ActionResult<AdminAcaoResultado>> RenomearTipo(int id, [FromBody] AdminTipoMoedaPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir as moedas");
                }

                var tipo = await _contexto.TiposMoeda.FirstOrDefaultAsync(t => t.Id == id);
                if (tipo == null)
                {
                    return NotFound("Tipo de moeda não encontrado.");
                }

                string? erro = await ValidarNomeAsync(pedido.Nome, idAtual: id);
                if (erro != null)
                {
                    return BadRequest(erro);
                }

                string antes = tipo.Nome;
                tipo.Nome = pedido.Nome.Trim();

                if (antes == tipo.Nome)
                {
                    return Ok(new AdminAcaoResultado { Mensagem = "Nada foi alterado." });
                }

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoMoeda + ": tipo " + antes + " → " + tipo.Nome,
                    "TiposMoeda", tipo.Id,
                    ComMotivo("Nome alterado.", pedido.Motivo));

                await _contexto.SaveChangesAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Moeda \"" + antes + "\" passou a chamar-se \"" + tipo.Nome + "\"." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar a moeda: " + ex.Message);
            }
        }

        // DELETE: api/admin/moedas/tipos/4?adminId=1
        [HttpDelete("tipos/{id:int}")]
        public async Task<ActionResult<AdminAcaoResultado>> ApagarTipo(int id, [FromQuery] int adminId, [FromQuery] string? motivo)
        {
            try
            {
                var admin = await ObterAdminAsync(adminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir as moedas");
                }

                var tipo = await _contexto.TiposMoeda.FirstOrDefaultAsync(t => t.Id == id);
                if (tipo == null)
                {
                    return NotFound("Tipo de moeda não encontrado.");
                }

                if (EhProtegida(id))
                {
                    return BadRequest("Gemas, Moedas e Bilhetes são usadas pelo código da plataforma — não podem ser apagadas.");
                }

                if (await _contexto.TransacoesMoeda.AnyAsync(t => t.TipoMoedaId == id) ||
                    await _contexto.Carteiras.AnyAsync(c => c.TipoMoedaId == id && c.Saldo != 0))
                {
                    return BadRequest("A moeda \"" + tipo.Nome + "\" já tem movimentos ou saldos — não pode ser apagada.");
                }

                if (await _contexto.RecompensasEvento.AnyAsync(r => r.TipoMoedaId == id))
                {
                    return BadRequest("A moeda \"" + tipo.Nome + "\" é usada em recompensas de eventos — mude-as primeiro.");
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                int carteiras = await _contexto.Carteiras.Where(c => c.TipoMoedaId == id).ExecuteDeleteAsync();
                _contexto.TiposMoeda.Remove(tipo);

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoMoeda + ": tipo " + tipo.Nome + " apagado",
                    "TiposMoeda", tipo.Id,
                    ComMotivo(carteiras + " carteiras vazias removidas.", motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Moeda \"" + tipo.Nome + "\" apagada." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao apagar a moeda: " + ex.Message);
            }
        }

        // POST: api/admin/moedas/oferta
        [HttpPost("oferta")]
        public async Task<ActionResult<AdminAcaoResultado>> Oferta([FromBody] AdminOfertaPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir as moedas");
                }

                var tipo = await _contexto.TiposMoeda.AsNoTracking().FirstOrDefaultAsync(t => t.Id == pedido.TipoMoedaId);
                if (tipo == null)
                {
                    return BadRequest("Escolha o tipo de moeda.");
                }

                if (pedido.Quantidade <= 0 || pedido.Quantidade > OfertaMaxima || decimal.Round(pedido.Quantidade, 2) != pedido.Quantidade)
                {
                    return BadRequest("A quantidade deve ser maior do que zero (no máximo 100 000, até 2 casas decimais).");
                }

                if (string.IsNullOrWhiteSpace(pedido.Motivo))
                {
                    return BadRequest("Indique o motivo da oferta (aparece no histórico de cada conta).");
                }

                string motivo = pedido.Motivo.Trim();
                string origem = Truncar("Admin: oferta - " + motivo, 50);
                decimal quantidade = pedido.Quantidade;
                var agora = DateTime.UtcNow;

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                // 1) carteira em falta criada a zero (contas criadas por script)
                await _contexto.Database.ExecuteSqlAsync(
                    $@"INSERT INTO CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
                       SELECT u.UtilizadorId, {tipo.Id}, 0 FROM Utilizadores u
                       WHERE u.IsAtivo = 1 AND u.NomeUtilizador <> 'Sistema'
                         AND NOT EXISTS (SELECT 1 FROM CarteirasUtilizador c
                                         WHERE c.UtilizadorId = u.UtilizadorId AND c.TipoMoedaId = {tipo.Id})");

                // 2) saldo
                int contas = await _contexto.Database.ExecuteSqlAsync(
                    $@"UPDATE c SET c.Saldo = c.Saldo + {quantidade}
                       FROM CarteirasUtilizador c
                       JOIN Utilizadores u ON u.UtilizadorId = c.UtilizadorId
                       WHERE c.TipoMoedaId = {tipo.Id} AND u.IsAtivo = 1 AND u.NomeUtilizador <> 'Sistema'");

                // 3) um movimento por conta, como qualquer outro ganho
                await _contexto.Database.ExecuteSqlAsync(
                    $@"INSERT INTO TransacoesMoeda (UtilizadorId, TipoMoedaId, Montante, TipoTransacao, Origem, DataCriacao)
                       SELECT UtilizadorId, {tipo.Id}, {quantidade}, 'Ganho', {origem}, {agora}
                       FROM Utilizadores WHERE IsAtivo = 1 AND NomeUtilizador <> 'Sistema'");

                // 4) aviso opcional
                int notificadas = 0;
                if (pedido.Notificar)
                {
                    string texto = "Recebeste " + quantidade.ToString("0.##", CultureInfo.GetCultureInfo("pt-PT")) + " " + tipo.Nome + ". " + motivo;
                    notificadas = await NotificarTodosAsync("Recompensa", "Presente da equipa WishBound", texto);
                }

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoMoeda + ": oferta de " + quantidade.ToString("0.##", CultureInfo.GetCultureInfo("pt-PT")) + " " + tipo.Nome,
                    "CarteirasUtilizador", tipo.Id,
                    "A " + contas + " contas ativas (total " + (quantidade * contas).ToString("0.##", CultureInfo.GetCultureInfo("pt-PT")) + ")" +
                    (notificadas > 0 ? ", notificadas" : "") + ". Motivo: " + motivo);

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = contas + " contas receberam " + quantidade.ToString("0.##", CultureInfo.GetCultureInfo("pt-PT")) + " " + tipo.Nome + "." +
                               (notificadas > 0 ? " Notificação enviada." : ""),
                    NovoValor = contas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao fazer a oferta: " + ex.Message);
            }
        }

        // ------------------------------------------------------------

        private static bool EhProtegida(int tipoMoedaId) =>
            tipoMoedaId == TiposMoedaIds.Gemas || tipoMoedaId == TiposMoedaIds.Moedas || tipoMoedaId == TiposMoedaIds.Bilhetes;

        private async Task<string?> ValidarNomeAsync(string? nome, int? idAtual)
        {
            nome = (nome ?? string.Empty).Trim();

            if (nome.Length < 2 || nome.Length > 30)
            {
                return "O nome deve ter entre 2 e 30 caracteres.";
            }

            bool repetido = await _contexto.TiposMoeda.AnyAsync(t => t.Nome == nome && (!idAtual.HasValue || t.Id != idAtual.Value));
            return repetido ? "Já existe uma moeda chamada \"" + nome + "\"." : null;
        }
    }
}
