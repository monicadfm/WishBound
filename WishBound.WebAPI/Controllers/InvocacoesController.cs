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
    ///
    /// NOTA (versão final): a tabela HistoricoInvocacoes exige o utilizador e
    /// o banner. Enquanto não existir autenticação, todas as invocações são
    /// registadas com o utilizador "Sistema" e o "Banner Permanente" criados
    /// pelo script de migração. Quando o login estiver feito, estas constantes
    /// serão substituídas pelo utilizador autenticado e pelo banner escolhido.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InvocacoesController : ControllerBase
    {
        // Ids fixos criados pelo script Database/01_Migracao_MiniParaFinal.sql
        private const int UtilizadorSistemaId = 1;
        private const int BannerPermanenteId = 1;

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

                // 3) Regista a invocação no histórico (INSERT)
                var invocacao = new Invocacao
                {
                    UtilizadorId = UtilizadorSistemaId,
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
