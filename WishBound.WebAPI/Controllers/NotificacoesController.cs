using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Notificações do utilizador (tabela Notificacoes). As linhas são
    /// escritas pelo sistema de amizade (subidas de nível e mensagens
    /// diárias das personagens); aqui só se leem e se marcam como lidas.
    /// Serve a app móvel e a futura página de notificações do site.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NotificacoesController : ControllerBase
    {
        private readonly WishBoundContext _contexto;

        public NotificacoesController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // ------------------------------------------------------------
        // GET: api/notificacoes?utilizadorId=5&limite=50&apenasNaoLidas=false
        // Mais recentes primeiro.
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<NotificacoesResposta>> Obter(
            [FromQuery] int utilizadorId,
            [FromQuery] int limite = 50,
            [FromQuery] bool apenasNaoLidas = false)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                limite = Math.Clamp(limite, 1, 200);

                var todas = _contexto.Notificacoes.AsNoTracking()
                    .Where(n => n.UtilizadorId == utilizadorId);

                var consulta = apenasNaoLidas ? todas.Where(n => !n.IsLida) : todas;

                var itens = await consulta
                    .OrderByDescending(n => n.DataCriacao)
                    .ThenByDescending(n => n.Id)
                    .Take(limite)
                    .Select(n => new NotificacaoResposta
                    {
                        Id = n.Id,
                        Tipo = n.Tipo,
                        Titulo = n.Titulo,
                        Mensagem = n.Mensagem,
                        IsLida = n.IsLida,
                        DataCriacao = n.DataCriacao
                    })
                    .ToListAsync();

                return Ok(new NotificacoesResposta
                {
                    NaoLidas = await todas.CountAsync(n => !n.IsLida),
                    Total = await todas.CountAsync(),
                    Itens = itens
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter as notificações: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/notificacoes/contagem?utilizadorId=5  -> número por ler
        // (leve, para o "sino" da página inicial)
        // ------------------------------------------------------------
        [HttpGet("contagem")]
        public async Task<ActionResult<int>> Contagem([FromQuery] int utilizadorId)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                var naoLidas = await _contexto.Notificacoes.AsNoTracking()
                    .CountAsync(n => n.UtilizadorId == utilizadorId && !n.IsLida);

                return Ok(naoLidas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao contar as notificações: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/notificacoes/lida  { UtilizadorId, NotificacaoId? }
        // NotificacaoId null = marca todas como lidas.
        // O filtro por UtilizadorId garante que ninguém marca notificações
        // de outra conta.
        // ------------------------------------------------------------
        [HttpPost("lida")]
        public async Task<IActionResult> MarcarLida([FromBody] MarcarLidaPedido pedido)
        {
            try
            {
                if (pedido.UtilizadorId <= 0)
                {
                    return BadRequest("Utilizador inválido.");
                }

                var consulta = _contexto.Notificacoes
                    .Where(n => n.UtilizadorId == pedido.UtilizadorId && !n.IsLida);

                if (pedido.NotificacaoId.HasValue)
                {
                    consulta = consulta.Where(n => n.Id == pedido.NotificacaoId.Value);
                }

                var marcadas = await consulta.ExecuteUpdateAsync(s => s.SetProperty(n => n.IsLida, true));

                if (pedido.NotificacaoId.HasValue)
                {
                    return Ok(marcadas == 0 ? "A notificação já estava lida (ou não existe)." : "Notificação marcada como lida.");
                }

                return Ok(marcadas == 0
                    ? "Não havia notificações por ler."
                    : marcadas + (marcadas == 1 ? " notificação marcada" : " notificações marcadas") + " como lida.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao marcar a notificação: " + ex.Message);
            }
        }
    }
}
