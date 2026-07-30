using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Sistema de invocação (gacha) simplificado:
    /// escolhe uma raridade de forma aleatória ponderada pelas probabilidades
    /// e devolve uma personagem dessa raridade. Guarda o resultado no histórico.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InvocacoesController : ControllerBase
    {
        private readonly WishBoundContext _contexto;

        public InvocacoesController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/invocacoes  (histórico - SELECT com JOIN)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invocacao>>> GetInvocacoes()
        {
            try
            {
                var historico = await _contexto.Invocacoes
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
        [HttpPost]
        public async Task<ActionResult<Personagem>> Invocar()
        {
            try
            {
                // Só considera raridades que tenham pelo menos uma personagem
                var raridades = await _contexto.Raridades
                    .Where(r => r.Personagens!.Any())
                    .Include(r => r.Personagens)
                    .ToListAsync();

                if (raridades.Count == 0)
                {
                    return NotFound("Não existem personagens para invocar.");
                }

                // 1) Escolha ponderada da raridade
                int somaPesos = raridades.Sum(r => r.Probabilidade);
                int sorteio = Random.Shared.Next(1, somaPesos + 1);

                Raridade raridadeEscolhida = raridades[0];
                int acumulado = 0;
                foreach (var raridade in raridades)
                {
                    acumulado += raridade.Probabilidade;
                    if (sorteio <= acumulado)
                    {
                        raridadeEscolhida = raridade;
                        break;
                    }
                }

                // 2) Escolha aleatória da personagem dentro da raridade
                var candidatas = raridadeEscolhida.Personagens!.ToList();
                var personagem = candidatas[Random.Shared.Next(candidatas.Count)];

                // 3) Regista a invocação no histórico (INSERT)
                var invocacao = new Invocacao
                {
                    PersonagemId = personagem.Id,
                    Data = DateTime.Now
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
