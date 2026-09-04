using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models.Economia;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Carteira do utilizador autenticado: saldos (Moedas e Bilhetes de
    /// invocação), calendário da recompensa diária, eventos de recompensas a
    /// decorrer e histórico de transações.
    /// Requer sessão iniciada — a carteira é sempre a do próprio.
    /// </summary>
    [Authorize]
    public class EconomiaController : Controller
    {
        private readonly WishBoundApiService _api;

        public EconomiaController(WishBoundApiService api)
        {
            _api = api;
        }

        // GET: /Economia  (a "Carteira")
        public async Task<IActionResult> Index()
        {
            try
            {
                var modelo = await _api.ObterEconomiaAsync(ObterUtilizadorId());
                return View(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter a carteira. Verifique se a WishBound.WebAPI está em execução.";
                return View(new EconomiaViewModel());
            }
        }

        // GET: /Economia/Transacoes
        public async Task<IActionResult> Transacoes()
        {
            try
            {
                var transacoes = await _api.ObterTransacoesAsync(ObterUtilizadorId());
                return View(transacoes);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter o histórico de transações. Verifique se a WishBound.WebAPI está em execução.";
                return View(new List<Transacao>());
            }
        }

        // POST: /Economia/ReceberDiaria  (recompensa de login diário)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceberDiaria()
        {
            try
            {
                var (resultado, erro) = await _api.ReceberLoginDiarioAsync(ObterUtilizadorId());

                if (resultado != null)
                {
                    TempData["Sucesso"] = resultado.Mensagem;
                }
                else
                {
                    TempData["Erro"] = string.IsNullOrWhiteSpace(erro)
                        ? "Não foi possível receber a recompensa diária."
                        : erro;
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível receber a recompensa. Verifique se a WishBound.WebAPI está em execução.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Economia/ResgatarEvento  (recompensa do dia num evento)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResgatarEvento(int bannerId)
        {
            try
            {
                var (resultado, erro) = await _api.ResgatarEventoAsync(ObterUtilizadorId(), bannerId);

                if (resultado != null)
                {
                    TempData["Sucesso"] = resultado.Mensagem;
                }
                else
                {
                    TempData["Erro"] = string.IsNullOrWhiteSpace(erro)
                        ? "Não foi possível resgatar a recompensa do evento."
                        : erro;
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível resgatar a recompensa. Verifique se a WishBound.WebAPI está em execução.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>Id do utilizador autenticado, guardado nos claims da sessão.</summary>
        private int ObterUtilizadorId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }
    }
}
