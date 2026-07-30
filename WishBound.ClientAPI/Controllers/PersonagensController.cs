using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Páginas públicas da coleção: lista de personagens (com pesquisa)
    /// e detalhes de uma personagem. Demonstra a operação SELECT.
    /// </summary>
    public class PersonagensController : Controller
    {
        private readonly WishBoundApiService _api;

        public PersonagensController(WishBoundApiService api)
        {
            _api = api;
        }

        // Página 2: Personagens (lista + pesquisa)
        public async Task<IActionResult> Index(string? pesquisa)
        {
            try
            {
                // Validação simples do dado de entrada vindo do URL
                pesquisa = pesquisa?.Trim();
                if (pesquisa != null && pesquisa.Length > 60)
                {
                    pesquisa = pesquisa.Substring(0, 60);
                }

                var personagens = await _api.ObterPersonagensAsync();

                if (!string.IsNullOrEmpty(pesquisa))
                {
                    personagens = personagens
                        .Where(p => p.Nome.Contains(pesquisa, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                ViewBag.Pesquisa = pesquisa;
                return View(personagens);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter as personagens. Verifique se a WishBound.WebAPI está em execução.";
                return View(new List<Personagem>());
            }
        }

        // Detalhes de uma personagem
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var personagem = await _api.ObterPersonagemAsync(id);

                if (personagem == null)
                {
                    TempData["Erro"] = "A personagem pedida não existe.";
                    return RedirectToAction(nameof(Index));
                }

                return View(personagem);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter a personagem. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
