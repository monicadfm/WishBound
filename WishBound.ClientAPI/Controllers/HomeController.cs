using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Models.Amizade;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Páginas gerais do site: Início, Sobre e página de erro.
    ///
    /// COMPANHEIRA: com sessão iniciada, a página inicial mostra a
    /// personagem que o utilizador escolheu para o receber, com uma
    /// saudação do conjunto de mensagens dela (muda com o nível de
    /// amizade). Só pode haver uma; escolhe-se aqui ou nos detalhes da
    /// personagem (POST /Colecao/Companheira).
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

                // Eventos a decorrer (banners temporários), com contagem
                // decrescente e personagens em destaque
                ViewBag.Eventos = (await _api.ObterBannersAsync())
                    .Where(b => !b.Permanente && !b.Terminado)
                    .ToList();
            }
            catch (Exception)
            {
                // Se a API estiver em baixo, a página inicial abre na mesma,
                // apenas sem a secção de destaques.
                ViewBag.Erro = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            // Companheira e check-in diário (só com sessão iniciada). Se a
            // API falhar (ex.: Migracao07 por correr), a página abre sem eles.
            if (User.Identity?.IsAuthenticated == true)
            {
                int utilizadorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                try
                {
                    ViewBag.Companheira = await _api.ObterCompanheiraAsync(utilizadorId);
                }
                catch (Exception)
                {
                    ViewBag.Companheira = null;
                }

                // Cartão "Check-in diário": a semana atual do calendário de 28
                // dias e o botão de receber, sem ir à Carteira.
                try
                {
                    ViewBag.Diaria = (await _api.ObterEconomiaAsync(utilizadorId)).RecompensaDiaria;
                }
                catch (Exception)
                {
                    ViewBag.Diaria = null;
                }
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
