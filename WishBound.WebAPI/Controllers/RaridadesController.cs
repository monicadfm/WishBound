using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Consulta das raridades disponíveis (tabela de apoio ao CRUD de personagens
    /// e ao sistema de invocação).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RaridadesController : ControllerBase
    {
        private readonly WishBoundContext _contexto;

        public RaridadesController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/raridades
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Raridade>>> GetRaridades()
        {
            try
            {
                var raridades = await _contexto.Raridades
                    .OrderBy(r => r.Id)
                    .ToListAsync();

                return Ok(raridades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter as raridades: " + ex.Message);
            }
        }
    }
}
