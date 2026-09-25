using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Área de gestão — CRUD das RARIDADES (nome, cor, probabilidade em %,
    /// escalão 1–5).
    ///
    /// Parte da classe GestaoController ([Authorize(Roles = "Admin")] está na
    /// outra parte, GestaoController.cs). Todos os POST têm antiforgery.
    /// </summary>
    public partial class GestaoController
    {
        // ============================================================
        //  RARIDADES
        // ============================================================

        // GET: /Gestao/Raridades
        public async Task<IActionResult> Raridades()
        {
            try
            {
                return View(await _api.AdminObterRaridadesAsync(ObterAdminId()));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter as raridades. " + MensagemApiEmBaixo;
                return View(new RaridadesViewModel());
            }
        }

        // POST: /Gestao/GuardarRaridade  (id vazio = criar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarRaridade(int? id, string nome, string cor, string percentagem, int ordem, string? motivo)
        {
            decimal? valor = LerDecimal(percentagem);
            if (valor == null || valor <= 0 || valor > 100)
            {
                TempData["Erro"] = "Indique a probabilidade em percentagem (ex.: 2.5 para 2,5%).";
                return RedirectToAction(nameof(Raridades));
            }

            return await ExecutarGestaoAsync(
                () => _api.AdminGuardarRaridadeAsync(ObterAdminId(), id, nome ?? string.Empty, cor ?? string.Empty,
                                                      Math.Round(valor.Value / 100m, 4), ordem, motivo),
                RedirectToAction(nameof(Raridades)));
        }

        // POST: /Gestao/ApagarRaridade/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApagarRaridade(int id, string? motivo)
        {
            return await ExecutarGestaoAsync(
                () => _api.AdminApagarRaridadeAsync(ObterAdminId(), id, motivo),
                RedirectToAction(nameof(Raridades)));
        }
    }
}
