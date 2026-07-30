using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Páginas gerais do site: Início, Sobre e página de erro.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly WishBoundApiService _api;

        public HomeController(WishBoundApiService api)
        {
            _api = api;
        }

        // Página 1: Início
        public async Task<IActionResult> Index()
        {
            var destaques = new List<Personagem>();

            try
            {
                var personagens = await _api.ObterPersonagensAsync();

                // Mostra em destaque as personagens mais raras
                destaques = personagens
                    .OrderByDescending(p => p.RaridadeId)
                    .Take(3)
                    .ToList();
            }
            catch (Exception)
            {
                // Se a API estiver em baixo, a página inicial abre na mesma,
                // apenas sem a secção de destaques.
                ViewBag.Erro = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            return View(destaques);
        }

        // Página 5: Sobre
        public IActionResult Sobre()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
