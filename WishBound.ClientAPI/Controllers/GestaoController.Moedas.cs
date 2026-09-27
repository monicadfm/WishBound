using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Área de gestão — MOEDAS: tipos de moeda, economia dos últimos 30 dias
    /// e oferta a todas as contas ativas.
    ///
    /// Parte da classe GestaoController ([Authorize(Roles = "Admin")] está na
    /// outra parte, GestaoController.cs). Todos os POST têm antiforgery.
    /// </summary>
    public partial class GestaoController
    {
        // ============================================================
        //  MOEDAS
        // ============================================================

        // GET: /Gestao/Moedas
        public async Task<IActionResult> Moedas()
        {
            try
            {
                return View(await _api.AdminObterMoedasAsync(ObterAdminId()));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter a economia. " + MensagemApiEmBaixo;
                return View(new MoedasViewModel());
            }
        }

        // POST: /Gestao/GuardarTipoMoeda  (id vazio = criar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarTipoMoeda(int? id, string nome, string? motivo)
        {
            return await ExecutarGestaoAsync(
                () => _api.AdminGuardarTipoMoedaAsync(ObterAdminId(), id, nome ?? string.Empty, motivo),
                RedirectToAction(nameof(Moedas)));
        }

        // POST: /Gestao/ApagarTipoMoeda/4
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApagarTipoMoeda(int id, string? motivo)
        {
            return await ExecutarGestaoAsync(
                () => _api.AdminApagarTipoMoedaAsync(ObterAdminId(), id, motivo),
                RedirectToAction(nameof(Moedas)));
        }

        // POST: /Gestao/Oferta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Oferta(int tipoMoedaId, string quantidade, bool notificar, string? motivo)
        {
            decimal? valor = LerDecimal(quantidade);
            if (valor == null || valor <= 0)
            {
                TempData["Erro"] = "Indique uma quantidade maior do que zero.";
                return RedirectToAction(nameof(Moedas));
            }

            return await ExecutarGestaoAsync(
                () => _api.AdminOfertaAsync(ObterAdminId(), tipoMoedaId, valor.Value, notificar, motivo),
                RedirectToAction(nameof(Moedas)));
        }
    }
}
