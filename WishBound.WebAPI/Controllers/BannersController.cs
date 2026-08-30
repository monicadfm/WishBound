using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Banners de invocação: o permanente e, no futuro, os de evento.
    /// O site usa esta lista para deixar escolher onde invocar — antes o
    /// banner estava fixo no código (Id 1).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BannersController : ControllerBase
    {
        private readonly WishBoundContext _contexto;

        public BannersController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/banners  (os que estão a decorrer)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BannerResposta>>> ObterBanners()
        {
            try
            {
                var agora = DateTime.UtcNow;

                var banners = await _contexto.Banners
                    .Where(b => b.IsAtivo && b.DataInicio <= agora && b.DataFim >= agora)
                    // Permanente primeiro, depois os eventos que acabam mais cedo
                    .OrderBy(b => b.TipoBanner == Banner.TipoStandard ? 0 : 1)
                    .ThenBy(b => b.DataFim)
                    .ToListAsync();

                var resposta = new List<BannerResposta>();

                foreach (var banner in banners)
                {
                    resposta.Add(new BannerResposta
                    {
                        Id = banner.Id,
                        Nome = banner.Nome,
                        Descricao = banner.Descricao,
                        TipoBanner = banner.TipoBanner,
                        ImagemUrl = banner.ImagemUrl,
                        DataFim = banner.DataFim,
                        Permanente = banner.TipoBanner == Banner.TipoStandard,
                        TotalPersonagens = await _contexto.BannerPersonagens
                            .CountAsync(bp => bp.BannerId == banner.Id &&
                                              _contexto.Personagens.Any(p => p.Id == bp.PersonagemId && p.IsAtivo))
                    });
                }

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter os banners: " + ex.Message);
            }
        }
    }
}
