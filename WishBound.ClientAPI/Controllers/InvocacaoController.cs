using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Página de invocação (gacha) e histórico de invocações.
    /// A invocação demonstra INSERT (regista no histórico) e SELECT.
    /// </summary>
    public class InvocacaoController : Controller
    {
        private readonly WishBoundApiService _api;

        public InvocacaoController(WishBoundApiService api)
        {
            _api = api;
        }

        // Página 3: Invocação
        public async Task<IActionResult> Index()
        {
            var modelo = new InvocacaoViewModel();

            try
            {
                modelo.Raridades = await _api.ObterRaridadesAsync();
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            return View(modelo);
        }

        // Botão "Invocar" (POST para evitar invocações acidentais por refresh/link)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invocar()
        {
            var modelo = new InvocacaoViewModel();

            try
            {
                modelo.Raridades = await _api.ObterRaridadesAsync();
                modelo.Resultado = await _api.InvocarAsync();

                if (modelo.Resultado == null)
                {
                    TempData["Erro"] = "A invocação falhou. Confirme que existem personagens na base de dados.";
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível realizar a invocação. Verifique se a WishBound.WebAPI está em execução.";
            }

            return View("Index", modelo);
        }

        // Página 4: Histórico
        public async Task<IActionResult> Historico()
        {
            try
            {
                var historico = await _api.ObterHistoricoAsync();
                return View(historico);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter o histórico. Verifique se a WishBound.WebAPI está em execução.";
                return View(new List<Invocacao>());
            }
        }
    }
}
