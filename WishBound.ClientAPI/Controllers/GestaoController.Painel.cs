using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Área de gestão — PAINEL (/Gestao): números do dia, banners ativos,
    /// alertas e últimas ações. Também tem os auxiliares comuns às páginas
    /// de gestão da plataforma: ExecutarGestaoAsync (corre uma ação e volta
    /// à página com a mensagem da API) e LerDecimal (lê "2.5" ou "2,5",
    /// independente da cultura do Windows).
    ///
    /// Parte da classe GestaoController ([Authorize(Roles = "Admin")] está na
    /// outra parte, GestaoController.cs). Todos os POST têm antiforgery.
    /// </summary>
    public partial class GestaoController
    {
        private const string MensagemApiEmBaixo = "Verifique se a WishBound.WebAPI está em execução.";

        // ============================================================
        //  PAINEL
        // ============================================================

        // GET: /Gestao
        public async Task<IActionResult> Index()
        {
            try
            {
                return View(await _api.AdminObterPainelAsync(ObterAdminId()));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter o painel. " + MensagemApiEmBaixo;
                return View(new PainelViewModel());
            }
        }

        // ============================================================
        //  Auxiliares
        // ============================================================

        /// <summary>Corre uma ação de gestão e volta à página indicada com a mensagem da API.</summary>
        private async Task<IActionResult> ExecutarGestaoAsync(Func<Task<(ResultadoAcaoAdmin? Resultado, string? Erro)>> acao, IActionResult destino)
        {
            try
            {
                var (resultado, erro) = await acao();

                if (resultado == null)
                {
                    TempData["Erro"] = "A API recusou a operação: " + erro;
                }
                else
                {
                    TempData["Sucesso"] = resultado.Mensagem;
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível concluir a operação. " + MensagemApiEmBaixo;
            }

            return destino;
        }

        /// <summary>Lê "2.5" ou "2,5" (independente da cultura do Windows); null se não for número.</summary>
        private static decimal? LerDecimal(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            texto = texto.Trim().Replace(" ", string.Empty).Replace(',', '.');
            return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) ? valor : null;
        }
    }
}
