using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Página de Eventos: os banners a decorrer (com contagem decrescente),
    /// as personagens em destaque (rate-up) e exclusivas de cada um e — com
    /// sessão iniciada — a participação do utilizador: garantias, estado do
    /// 50/50, invocações feitas, personagens já obtidas e as recompensas
    /// diárias do evento (que se resgatam na Carteira).
    ///
    /// Abre sem sessão (mostra os banners e as regras); os dados pessoais
    /// só aparecem com sessão iniciada.
    /// </summary>
    public class EventosController : Controller
    {
        private readonly WishBoundApiService _api;

        public EventosController(WishBoundApiService api)
        {
            _api = api;
        }

        // GET /Eventos?bannerId=3
        public async Task<IActionResult> Index(int bannerId = 0)
        {
            var modelo = new EventosViewModel();

            try
            {
                modelo.Banners = await _api.ObterBannersAsync(ObterUtilizadorId());

                // Por omissão abre o primeiro evento (o permanente fica em
                // último; sem eventos a decorrer abre o permanente).
                var escolhido = modelo.Banners.FirstOrDefault(b => b.Id == bannerId)
                                ?? modelo.Banners.FirstOrDefault(b => !b.Permanente)
                                ?? modelo.Banners.FirstOrDefault();

                if (escolhido != null)
                {
                    modelo.Detalhe = await _api.ObterBannerAsync(escolhido.Id, ObterUtilizadorId());
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            return View(modelo);
        }

        /// <summary>Id do utilizador autenticado (0 sem sessão — a API devolve só os dados públicos).</summary>
        private int ObterUtilizadorId()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return 0;
            }

            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }
    }
}
