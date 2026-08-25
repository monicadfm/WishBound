using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Página de invocação (gacha) e histórico de invocações.
    /// Requer sessão iniciada: cada invocação é registada em nome do
    /// utilizador autenticado, entra na coleção dele e o histórico mostrado
    /// é apenas o seu.
    /// </summary>
    [Authorize]
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

                var (resultado, erro) = await _api.InvocarAsync(ObterUtilizadorId());
                modelo.Resultado = resultado;

                if (resultado == null)
                {
                    // A API explica o motivo (ex.: coleção cheia); só usamos a
                    // mensagem genérica quando não vem nenhuma.
                    TempData["Erro"] = string.IsNullOrWhiteSpace(erro)
                        ? "A invocação falhou. Confirme que existem personagens na base de dados."
                        : erro;
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível realizar a invocação. Verifique se a WishBound.WebAPI está em execução.";
            }

            return View("Index", modelo);
        }

        // Página 4: Histórico (apenas as invocações do utilizador autenticado)
        public async Task<IActionResult> Historico()
        {
            try
            {
                var historico = await _api.ObterHistoricoAsync(ObterUtilizadorId());
                return View(historico);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter o histórico. Verifique se a WishBound.WebAPI está em execução.";
                return View(new List<Invocacao>());
            }
        }

        /// <summary>Id do utilizador autenticado, guardado nos claims da sessão.</summary>
        private int ObterUtilizadorId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }
    }
}
