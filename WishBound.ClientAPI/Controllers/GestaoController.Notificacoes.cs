using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Área de gestão — NOTIFICAÇÕES: enviar a uma conta ou a todas e limpar
    /// as antigas.
    ///
    /// Parte da classe GestaoController ([Authorize(Roles = "Admin")] está na
    /// outra parte, GestaoController.cs). Todos os POST têm antiforgery.
    /// </summary>
    public partial class GestaoController
    {
        // ============================================================
        //  NOTIFICAÇÕES
        // ============================================================

        // GET: /Gestao/Notificacoes?utilizadorId=5
        public async Task<IActionResult> Notificacoes(int? utilizadorId)
        {
            try
            {
                var modelo = await _api.AdminObterNotificacoesAsync(ObterAdminId());

                if (utilizadorId.HasValue)
                {
                    var conta = await _api.AdminObterContaAsync(ObterAdminId(), utilizadorId.Value);
                    if (conta != null)
                    {
                        modelo.UtilizadorId = conta.Conta.Id;
                        modelo.UtilizadorNome = conta.Conta.NomeUtilizador;
                    }
                }

                return View(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter as notificações. " + MensagemApiEmBaixo;
                return View(new NotificacoesAdminViewModel());
            }
        }

        // POST: /Gestao/EnviarNotificacao   (destino: todos | conta)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarNotificacao(string tipo, string titulo, string mensagem, string destino, int? utilizadorId, string? motivo)
        {
            int? alvo = destino == "conta" ? utilizadorId : null;

            if (destino == "conta" && (!alvo.HasValue || alvo.Value <= 0))
            {
                TempData["Erro"] = "Indique o Id da conta que deve receber a notificação.";
                return RedirectToAction(nameof(Notificacoes));
            }

            return await ExecutarGestaoAsync(
                () => _api.AdminEnviarNotificacaoAsync(ObterAdminId(), tipo, titulo ?? string.Empty, mensagem ?? string.Empty, alvo, motivo),
                RedirectToAction(nameof(Notificacoes)));
        }

        // POST: /Gestao/LimparNotificacoes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LimparNotificacoes(int dias, bool apenasLidas, string? motivo)
        {
            return await ExecutarGestaoAsync(
                () => _api.AdminLimparNotificacoesAsync(ObterAdminId(), dias, apenasLidas, motivo),
                RedirectToAction(nameof(Notificacoes)));
        }
    }
}
