using System.Net.Http.Json;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Services
{
    /// <summary>
    /// Pedidos do PAINEL da gestão (api/admin/painel) + os auxiliares
    /// comuns às páginas de gestão da plataforma (EnviarAdminAsync, Motivo).
    /// Todos os pedidos levam o Id do administrador com sessão iniciada; a
    /// API confirma que é uma conta de administrador ativa.
    /// </summary>
    public partial class WishBoundApiService
    {
        // ---------- Painel ----------


        public async Task<PainelViewModel> AdminObterPainelAsync(int adminId)
        {
            return await _http.GetFromJsonAsync<PainelViewModel>("api/admin/painel?adminId=" + adminId)
                   ?? new PainelViewModel();
        }

        // ---------- Auxiliares ----------

        /// <summary>POST/PUT/DELETE de gestão: resultado ou texto do erro da API.</summary>
        private async Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> EnviarAdminAsync(HttpMethod metodo, string url, object? corpo)
        {
            using var pedido = new HttpRequestMessage(metodo, url);
            if (corpo != null)
            {
                pedido.Content = JsonContent.Create(corpo);
            }

            var resposta = await _http.SendAsync(pedido);

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<ResultadoAcaoAdmin>(), null);
        }

        private static string Motivo(string? motivo) =>
            string.IsNullOrWhiteSpace(motivo) ? string.Empty : "&motivo=" + Uri.EscapeDataString(motivo.Trim());
    }
}
