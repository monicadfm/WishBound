using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// GESTÃO DAS NOTIFICAÇÕES (api/admin/notificacoes).
    ///
    ///   GET  api/admin/notificacoes?adminId=      - contagens por tipo, as 50 mais
    ///                                               recentes e os últimos envios feitos
    ///                                               por administradores
    ///   POST api/admin/notificacoes/enviar        - envia a UMA conta ou a TODAS as ativas
    ///   POST api/admin/notificacoes/limpar        - apaga as antigas (por omissão só as lidas)
    ///
    /// Os tipos aceites são os do CHECK da tabela: Evento, Banner,
    /// Recompensa e LoginDiario (MensagemPersonagem fica reservado às
    /// personagens — o sistema de amizade é que as escreve). As
    /// notificações aparecem no separador Notificações da app móvel e no
    /// contador de notificações do Início; a app lê-as de api/notificacoes.
    /// Cada envio/limpeza fica em LogsAdministrador (categoria "Notificacao").
    /// </summary>
    [Route("api/admin/notificacoes")]
    [ApiController]
    public class AdminNotificacoesController : AdminBaseController
    {
        public static readonly string[] TiposPermitidos = { "Evento", "Banner", "Recompensa", "LoginDiario" };

        public AdminNotificacoesController(WishBoundContext contexto) : base(contexto)
        {
        }

        // GET: api/admin/notificacoes?adminId=1
        [HttpGet]
        public async Task<ActionResult<AdminNotificacoesResposta>> Obter([FromQuery] int adminId)
        {
            try
            {
                if (await ObterAdminAsync(adminId) == null)
                {
                    return ApenasAdmins("gerir as notificações");
                }

                var porTipo = await _contexto.Notificacoes.AsNoTracking()
                    .GroupBy(n => n.Tipo)
                    .Select(g => new AdminContagemTipo { Tipo = g.Key, Total = g.Count(), NaoLidas = g.Count(n => !n.IsLida) })
                    .ToListAsync();

                var recentes = await _contexto.Notificacoes.AsNoTracking()
                    .OrderByDescending(n => n.DataCriacao).ThenByDescending(n => n.Id)
                    .Take(50)
                    .Join(_contexto.Utilizadores, n => n.UtilizadorId, u => u.Id, (n, u) => new AdminNotificacaoLinha
                    {
                        Id = n.Id,
                        UtilizadorId = n.UtilizadorId,
                        UtilizadorNome = u.NomeUtilizador,
                        Tipo = n.Tipo,
                        Titulo = n.Titulo,
                        Mensagem = n.Mensagem,
                        IsLida = n.IsLida,
                        Data = n.DataCriacao
                    })
                    .ToListAsync();

                return Ok(new AdminNotificacoesResposta
                {
                    Total = porTipo.Sum(t => t.Total),
                    NaoLidas = porTipo.Sum(t => t.NaoLidas),
                    ContasAtivas = await _contexto.Utilizadores.CountAsync(u => u.IsAtivo && u.NomeUtilizador != "Sistema"),
                    PorTipo = porTipo.OrderByDescending(t => t.Total).ToList(),
                    Recentes = recentes.OrderByDescending(n => n.Data).ThenByDescending(n => n.Id).ToList(),
                    Envios = await ObterAcoesRecentesAsync(20, LogAdministrador.AcaoNotificacao),
                    TiposPermitidos = TiposPermitidos.ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter as notificações: " + ex.Message);
            }
        }

        // POST: api/admin/notificacoes/enviar
        [HttpPost("enviar")]
        public async Task<ActionResult<AdminAcaoResultado>> Enviar([FromBody] AdminNotificacaoPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("enviar notificações");
                }

                if (!TiposPermitidos.Contains(pedido.Tipo))
                {
                    return BadRequest("Tipo inválido — use Evento, Banner, Recompensa ou LoginDiario.");
                }

                string titulo = (pedido.Titulo ?? string.Empty).Trim();
                string mensagem = (pedido.Mensagem ?? string.Empty).Trim();

                if (titulo.Length < 3 || titulo.Length > 100)
                {
                    return BadRequest("O título deve ter entre 3 e 100 caracteres.");
                }

                if (mensagem.Length < 3 || mensagem.Length > 255)
                {
                    return BadRequest("A mensagem deve ter entre 3 e 255 caracteres.");
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                int enviadas;
                string destino;

                if (pedido.UtilizadorId.HasValue && pedido.UtilizadorId.Value > 0)
                {
                    var conta = await _contexto.Utilizadores.AsNoTracking().FirstOrDefaultAsync(u => u.Id == pedido.UtilizadorId.Value);
                    if (conta == null)
                    {
                        return BadRequest("A conta indicada não existe.");
                    }

                    _contexto.Notificacoes.Add(new Notificacao
                    {
                        UtilizadorId = conta.Id,
                        Tipo = pedido.Tipo,
                        Titulo = titulo,
                        Mensagem = mensagem,
                        IsLida = false,
                        DataCriacao = DateTime.UtcNow
                    });

                    enviadas = 1;
                    destino = conta.NomeUtilizador;
                }
                else
                {
                    enviadas = await NotificarTodosAsync(pedido.Tipo, titulo, mensagem);
                    destino = "todas as contas ativas";
                }

                RegistarAcao(admin.Id, pedido.UtilizadorId > 0 ? pedido.UtilizadorId : null,
                    LogAdministrador.AcaoNotificacao + ": " + pedido.Tipo + " \"" + titulo + "\"",
                    "Notificacoes", null,
                    ComMotivo("Enviada a " + destino + " (" + enviadas + "): " + mensagem, pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = enviadas == 1 ? "Notificação enviada a " + destino + "." : "Notificação enviada a " + enviadas + " contas.",
                    NovoValor = enviadas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao enviar a notificação: " + ex.Message);
            }
        }

        // POST: api/admin/notificacoes/limpar
        [HttpPost("limpar")]
        public async Task<ActionResult<AdminAcaoResultado>> Limpar([FromBody] AdminLimparNotificacoesPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir as notificações");
                }

                if (pedido.Dias < 1 || pedido.Dias > 3650)
                {
                    return BadRequest("Indique um número de dias entre 1 e 3650.");
                }

                var limite = DateTime.UtcNow.AddDays(-pedido.Dias);

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                var consulta = _contexto.Notificacoes.Where(n => n.DataCriacao < limite);
                if (pedido.ApenasLidas)
                {
                    consulta = consulta.Where(n => n.IsLida);
                }

                int apagadas = await consulta.ExecuteDeleteAsync();

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoNotificacao + ": limpeza",
                    "Notificacoes", null,
                    ComMotivo(apagadas + " notificações " + (pedido.ApenasLidas ? "lidas " : "") + "com mais de " + pedido.Dias + " dias apagadas.", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado { Mensagem = apagadas + " notificações apagadas.", NovoValor = apagadas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao limpar as notificações: " + ex.Message);
            }
        }
    }
}
