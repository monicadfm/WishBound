using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Controller principal do CRUD de Personagens.
    ///   GET    api/personagens        -> SELECT (todas)
    ///   GET    api/personagens/5      -> SELECT (uma)
    ///   POST   api/personagens        -> INSERT
    ///   PUT    api/personagens/5      -> UPDATE
    ///   DELETE api/personagens/5      -> DELETE
    /// Todas as ações usam try-catch (critério de robustez).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PersonagensController : ControllerBase
    {
        private readonly WishBoundContext _contexto;

        public PersonagensController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/personagens  (SELECT)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Personagem>>> GetPersonagens()
        {
            try
            {
                var personagens = await _contexto.Personagens
                    .Include(p => p.Raridade)
                    .OrderBy(p => p.RaridadeId)
                    .ThenBy(p => p.Nome)
                    .ToListAsync();

                return Ok(personagens);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter as personagens: " + ex.Message);
            }
        }

        // GET: api/personagens/5  (SELECT por Id)
        [HttpGet("{id}")]
        public async Task<ActionResult<Personagem>> GetPersonagem(int id)
        {
            try
            {
                var personagem = await _contexto.Personagens
                    .Include(p => p.Raridade)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (personagem == null)
                {
                    return NotFound("Não existe nenhuma personagem com o Id " + id + ".");
                }

                return Ok(personagem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a personagem: " + ex.Message);
            }
        }

        // POST: api/personagens  (INSERT)
        [HttpPost]
        public async Task<ActionResult<Personagem>> PostPersonagem(Personagem personagem)
        {
            try
            {
                // Validação de dados de entrada (Data Annotations)
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Valida se a raridade indicada existe
                bool raridadeExiste = await _contexto.Raridades.AnyAsync(r => r.Id == personagem.RaridadeId);
                if (!raridadeExiste)
                {
                    return BadRequest("A raridade indicada não existe.");
                }

                personagem.Id = 0;
                personagem.Raridade = null;
                personagem.DataCriacao = DateTime.Now;

                _contexto.Personagens.Add(personagem);
                await _contexto.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPersonagem), new { id = personagem.Id }, personagem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao criar a personagem: " + ex.Message);
            }
        }

        // PUT: api/personagens/5  (UPDATE)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPersonagem(int id, Personagem personagem)
        {
            try
            {
                if (id != personagem.Id)
                {
                    return BadRequest("O Id do URL não corresponde ao Id da personagem enviada.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existente = await _contexto.Personagens.FindAsync(id);
                if (existente == null)
                {
                    return NotFound("Não existe nenhuma personagem com o Id " + id + ".");
                }

                bool raridadeExiste = await _contexto.Raridades.AnyAsync(r => r.Id == personagem.RaridadeId);
                if (!raridadeExiste)
                {
                    return BadRequest("A raridade indicada não existe.");
                }

                // Atualiza apenas os campos editáveis
                existente.Nome = personagem.Nome;
                existente.Descricao = personagem.Descricao;
                existente.ImagemUrl = personagem.ImagemUrl;
                existente.RaridadeId = personagem.RaridadeId;

                await _contexto.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao atualizar a personagem: " + ex.Message);
            }
        }

        // DELETE: api/personagens/5  (DELETE)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePersonagem(int id)
        {
            try
            {
                var personagem = await _contexto.Personagens.FindAsync(id);
                if (personagem == null)
                {
                    return NotFound("Não existe nenhuma personagem com o Id " + id + ".");
                }

                _contexto.Personagens.Remove(personagem);
                await _contexto.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao apagar a personagem: " + ex.Message);
            }
        }
    }
}
