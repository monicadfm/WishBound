using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Sistema de invocação (gacha):
    /// escolhe uma raridade de forma aleatória ponderada pelas probabilidades
    /// e devolve uma personagem dessa raridade. Guarda o resultado no histórico.
    ///
    /// Desde a versão com autenticação, cada invocação pertence a um utilizador
    /// real: o site envia o Id do utilizador autenticado e o histórico é
    /// consultado POR utilizador. O banner continua a ser o "Banner Permanente"
    /// (Id 1) criado pela migração — a escolha de banner chega com a
    /// funcionalidade de eventos/banners temporários.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InvocacoesController : ControllerBase
    {
        // Id fixo criado pelo script Database/Migracao01.sql.
        // Será substituído pelo banner escolhido quando existirem banners de evento.
        private const int BannerPermanenteId = 1;

        private readonly WishBoundContext _contexto;

        public InvocacoesController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/invocacoes?utilizadorId=5  (histórico DO utilizador - SELECT com JOIN)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invocacao>>> GetInvocacoes([FromQuery] int utilizadorId)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                var historico = await _contexto.Invocacoes
                    .Where(i => i.UtilizadorId == utilizadorId)
                    .Include(i => i.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .OrderByDescending(i => i.Data)
                    .Take(50)
                    .ToListAsync();

                return Ok(historico);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o histórico de invocações: " + ex.Message);
            }
        }

        // POST: api/invocacoes  (realiza uma invocação - INSERT)
        // O corpo indica QUEM está a invocar: { "utilizadorId": 5 }
        [HttpPost]
        public async Task<ActionResult<Personagem>> Invocar([FromBody] InvocacaoPedido pedido)
        {
            try
            {
                // O utilizador tem de existir e estar ativo — protege contra
                // pedidos com Ids inventados (o histórico tem FK para Utilizadores).
                var utilizador = await _contexto.Utilizadores.FindAsync(pedido.UtilizadorId);
                if (utilizador == null || !utilizador.IsAtivo)
                {
                    return BadRequest("Utilizador inválido para invocar.");
                }

                // Só considera raridades com pelo menos uma personagem ativa
                var raridades = await _contexto.Raridades
                    .Where(r => r.Personagens!.Any(p => p.IsAtivo))
                    .Include(r => r.Personagens!.Where(p => p.IsAtivo))
                    .OrderBy(r => r.Ordem)
                    .ToListAsync();

                if (raridades.Count == 0)
                {
                    return NotFound("Não existem personagens para invocar.");
                }

                // 1) Escolha ponderada da raridade
                //    As probabilidades são frações (ex.: 0.55 = 55%). O sorteio
                //    gera um número entre 0 e a soma dos pesos, e percorre as
                //    raridades acumulando até o ultrapassar.
                decimal somaPesos = raridades.Sum(r => r.Probabilidade);
                decimal sorteio = (decimal)Random.Shared.NextDouble() * somaPesos;

                Raridade raridadeEscolhida = raridades[0];
                decimal acumulado = 0;
                foreach (var raridade in raridades)
                {
                    acumulado += raridade.Probabilidade;
                    if (sorteio < acumulado)
                    {
                        raridadeEscolhida = raridade;
                        break;
                    }
                }

                // 2) Escolha aleatória da personagem dentro da raridade
                var candidatas = raridadeEscolhida.Personagens!.ToList();
                var personagem = candidatas[Random.Shared.Next(candidatas.Count)];

                // 3) Regista a invocação no histórico (INSERT) — agora em nome
                //    do utilizador autenticado que o site enviou.
                var invocacao = new Invocacao
                {
                    UtilizadorId = utilizador.Id,
                    BannerId = BannerPermanenteId,
                    PersonagemId = personagem.Id,
                    RaridadeId = raridadeEscolhida.Id,
                    PityAtivado = false,
                    Data = DateTime.UtcNow
                };

                _contexto.Invocacoes.Add(invocacao);
                await _contexto.SaveChangesAsync();

                return Ok(personagem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao realizar a invocação: " + ex.Message);
            }
        }
    }
}
