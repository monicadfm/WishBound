using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Área de gestão — ESTATÍSTICAS (7/30/90/365 dias, com as 3 vistas da
    /// base de dados) e EXPORTAR (PDF ou XML de estatísticas, contas,
    /// personagens, banners, registo de ações e transações).
    ///
    /// Parte da classe GestaoController ([Authorize(Roles = "Admin")] está na
    /// outra parte, GestaoController.cs). Todos os POST têm antiforgery.
    /// </summary>
    public partial class GestaoController
    {
        // ============================================================
        //  ESTATÍSTICAS E EXPORTAÇÃO
        // ============================================================

        // GET: /Gestao/Estatisticas?dias=30
        public async Task<IActionResult> Estatisticas(int dias = 30)
        {
            dias = dias is 7 or 30 or 90 or 365 ? dias : 30;

            try
            {
                return View(await _api.AdminObterEstatisticasAsync(ObterAdminId(), dias));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível calcular as estatísticas. " + MensagemApiEmBaixo;
                return View(new EstatisticasViewModel { Dias = dias });
            }
        }

        // GET: /Gestao/Exportar?conjunto=utilizadores&formato=pdf&dias=30
        public async Task<IActionResult> Exportar(string conjunto, string formato = "pdf", int dias = 30)
        {
            try
            {
                var (conteudo, tipo, nome, erro) = await _api.AdminExportarAsync(ObterAdminId(), conjunto, formato, dias);

                if (conteudo == null)
                {
                    TempData["Erro"] = "A API recusou a exportação: " + erro;
                    return RedirectToAction(nameof(Estatisticas), new { dias });
                }

                return File(conteudo, tipo!, nome);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível exportar. " + MensagemApiEmBaixo;
                return RedirectToAction(nameof(Estatisticas), new { dias });
            }
        }
    }
}
