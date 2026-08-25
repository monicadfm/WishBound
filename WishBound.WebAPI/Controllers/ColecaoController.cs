using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Coleção pessoal de cada utilizador (tabela ColecaoUtilizador):
    ///   - consulta da coleção com ordenação e filtro de favoritos (SELECT);
    ///   - detalhe de uma personagem da coleção (SELECT);
    ///   - marcar/desmarcar favorita (UPDATE);
    ///   - libertar cópias repetidas para ganhar espaço (UPDATE).
    ///
    /// As personagens entram aqui pelo sistema de invocação
    /// (ver InvocacoesController), que também verifica se ainda há espaço.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ColecaoController : ControllerBase
    {
        private readonly WishBoundContext _contexto;

        public ColecaoController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // ------------------------------------------------------------
        // GET: api/colecao?utilizadorId=5&ordenar=raridade&favoritos=false
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<ColecaoResposta>> ObterColecao(
            [FromQuery] int utilizadorId,
            [FromQuery] string? ordenar = "raridade",
            [FromQuery] bool favoritos = false)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                var consulta = _contexto.Colecoes
                    .Where(c => c.UtilizadorId == utilizadorId)
                    .Include(c => c.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .AsQueryable();

                if (favoritos)
                {
                    consulta = consulta.Where(c => c.IsFavorito);
                }

                // ORDER BY feito na base de dados (não em memória)
                consulta = (ordenar ?? "raridade").ToLower() switch
                {
                    "nome" => consulta.OrderBy(c => c.Personagem!.Nome),
                    "data" => consulta.OrderByDescending(c => c.DataObtencao),
                    // Por omissão: das raridades mais altas para as mais baixas
                    _ => consulta
                        .OrderByDescending(c => c.Personagem!.Raridade!.Ordem)
                        .ThenBy(c => c.Personagem!.Nome)
                };

                var itens = await consulta.ToListAsync();

                var inventario = await ObterInventarioAsync(utilizadorId);

                // O espaço ocupado conta TODAS as cópias, incluindo as repetidas
                int ocupado = await _contexto.Colecoes
                    .Where(c => c.UtilizadorId == utilizadorId)
                    .SumAsync(c => (int?)c.Quantidade) ?? 0;

                int distintas = await _contexto.Colecoes
                    .CountAsync(c => c.UtilizadorId == utilizadorId && c.Personagem!.IsAtivo);

                return Ok(new ColecaoResposta
                {
                    Itens = itens.Select(ParaResposta).ToList(),
                    Ocupado = ocupado,
                    Capacidade = inventario.CapacidadeTotal,
                    PersonagensDistintas = distintas,
                    PersonagensExistentes = await _contexto.Personagens.CountAsync(p => p.IsAtivo)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a coleção: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/colecao/item?utilizadorId=5&personagemId=3
        // ------------------------------------------------------------
        [HttpGet("item")]
        public async Task<ActionResult<ItemColecaoResposta>> ObterItem(
            [FromQuery] int utilizadorId, [FromQuery] int personagemId)
        {
            try
            {
                if (utilizadorId <= 0 || personagemId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador e a personagem.");
                }

                var item = await _contexto.Colecoes
                    .Include(c => c.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .FirstOrDefaultAsync(c => c.UtilizadorId == utilizadorId && c.PersonagemId == personagemId);

                if (item == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                return Ok(ParaResposta(item));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a personagem da coleção: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/colecao/favorito  (UPDATE)
        // ------------------------------------------------------------
        [HttpPost("favorito")]
        public async Task<IActionResult> MarcarFavorito(FavoritoPedido pedido)
        {
            try
            {
                if (pedido.UtilizadorId <= 0 || pedido.PersonagemId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador e a personagem.");
                }

                var item = await _contexto.Colecoes.FirstOrDefaultAsync(
                    c => c.UtilizadorId == pedido.UtilizadorId && c.PersonagemId == pedido.PersonagemId);

                if (item == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                item.IsFavorito = pedido.Favorito;
                await _contexto.SaveChangesAsync();

                return Ok(pedido.Favorito
                    ? "Personagem marcada como favorita."
                    : "Personagem removida dos favoritos.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar o favorito: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/colecao/libertar  (UPDATE - gestão de duplicados)
        // ------------------------------------------------------------
        // Liberta cópias repetidas para recuperar espaço. A primeira cópia
        // nunca é libertada: a personagem continua na coleção.
        [HttpPost("libertar")]
        public async Task<IActionResult> LibertarDuplicados(LibertarPedido pedido)
        {
            try
            {
                if (pedido.UtilizadorId <= 0 || pedido.PersonagemId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador e a personagem.");
                }

                var item = await _contexto.Colecoes
                    .Include(c => c.Personagem)
                    .FirstOrDefaultAsync(
                        c => c.UtilizadorId == pedido.UtilizadorId && c.PersonagemId == pedido.PersonagemId);

                if (item == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                int repetidas = item.Quantidade - 1;
                if (repetidas <= 0)
                {
                    return BadRequest("Só tem uma cópia desta personagem: não há repetidas para libertar.");
                }

                // 0 ou menos = libertar todas as repetidas
                int aLibertar = pedido.Quantidade <= 0
                    ? repetidas
                    : Math.Min(pedido.Quantidade, repetidas);

                item.Quantidade -= aLibertar;
                await _contexto.SaveChangesAsync();

                return Ok(aLibertar == 1
                    ? "Libertada 1 cópia repetida de " + (item.Personagem?.Nome ?? "personagem") + "."
                    : "Libertadas " + aLibertar + " cópias repetidas.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao libertar as cópias repetidas: " + ex.Message);
            }
        }

        // ============================================================
        //  Métodos auxiliares
        // ============================================================

        /// <summary>
        /// Inventário do utilizador. Isto é uma CONSULTA: se a linha ainda não
        /// existir (contas criadas por script), devolve a capacidade base sem
        /// escrever nada na base de dados — a linha é criada na primeira
        /// invocação (ver InvocacoesController).
        /// </summary>
        private async Task<Inventario> ObterInventarioAsync(int utilizadorId)
        {
            return await _contexto.Inventarios.FindAsync(utilizadorId)
                   ?? new Inventario { UtilizadorId = utilizadorId, CapacidadeBase = 100, CapacidadeExtra = 0 };
        }

        /// <summary>Converte a linha da coleção no DTO enviado ao site.</summary>
        private static ItemColecaoResposta ParaResposta(ItemColecao item)
        {
            return new ItemColecaoResposta
            {
                PersonagemId = item.PersonagemId,
                Nome = item.Personagem?.Nome ?? "Personagem removida",
                Descricao = item.Personagem?.Descricao,
                ImagemUrl = item.Personagem?.ImagemUrl,
                RaridadeId = item.Personagem?.RaridadeId ?? 0,
                RaridadeNome = item.Personagem?.Raridade?.Nome ?? "Desconhecida",
                RaridadeCor = item.Personagem?.Raridade?.Cor,
                RaridadeOrdem = item.Personagem?.Raridade?.Ordem ?? 0,
                Quantidade = item.Quantidade,
                IsFavorito = item.IsFavorito,
                DataObtencao = item.DataObtencao
            };
        }
    }
}
