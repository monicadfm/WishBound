using System.Net.Http.Json;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Services
{
    /// <summary>
    /// Pedidos da GESTÃO DAS NOTIFICAÇÕES (api/admin/notificacoes).
    /// Todos os pedidos levam o Id do administrador com sessão iniciada; a
    /// API confirma que é uma conta de administrador ativa.
    /// </summary>
    public partial class WishBoundApiService
    {
        // ---------- Notificações ----------

        public async Task<NotificacoesAdminViewModel> AdminObterNotificacoesAsync(int adminId)
        {
            return await _http.GetFromJsonAsync<NotificacoesAdminViewModel>("api/admin/notificacoes?adminId=" + adminId)
                   ?? new NotificacoesAdminViewModel();
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminEnviarNotificacaoAsync(
            int adminId, string tipo, string titulo, string mensagem, int? utilizadorId, string? motivo)
        {
            return EnviarAdminAsync(HttpMethod.Post, "api/admin/notificacoes/enviar", new
            {
                AdminId = adminId,
                Tipo = tipo,
                Titulo = titulo,
                Mensagem = mensagem,
                UtilizadorId = utilizadorId,
                Motivo = motivo
            });
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminLimparNotificacoesAsync(int adminId, int dias, bool apenasLidas, string? motivo)
        {
            return EnviarAdminAsync(HttpMethod.Post, "api/admin/notificacoes/limpar", new
            {
                AdminId = adminId,
                Dias = dias,
                ApenasLidas = apenasLidas,
                Motivo = motivo
            });
        }
    }
}
