using System.Net.Http.Json;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Services
{
    /// <summary>
    /// Pedidos das ESTATÍSTICAS e das EXPORTAÇÕES (api/admin/estatisticas,
    /// api/admin/exportar).
    /// Todos os pedidos levam o Id do administrador com sessão iniciada; a
    /// API confirma que é uma conta de administrador ativa.
    /// </summary>
    public partial class WishBoundApiService
    {
        // ---------- Estatísticas e exportação ----------

        public async Task<EstatisticasViewModel> AdminObterEstatisticasAsync(int adminId, int dias)
        {
            return await _http.GetFromJsonAsync<EstatisticasViewModel>("api/admin/estatisticas?adminId=" + adminId + "&dias=" + dias)
                   ?? new EstatisticasViewModel();
        }

        /// <summary>Ficheiro exportado (PDF/XML) — conteúdo, tipo e nome — ou o erro da API.</summary>
        public async Task<(byte[]? Conteudo, string? Tipo, string? Nome, string? Erro)> AdminExportarAsync(
            int adminId, string conjunto, string formato, int dias)
        {
            var resposta = await _http.GetAsync("api/admin/exportar?adminId=" + adminId +
                                                "&conjunto=" + Uri.EscapeDataString(conjunto) +
                                                "&formato=" + Uri.EscapeDataString(formato) +
                                                "&dias=" + dias);

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, null, null, await resposta.Content.ReadAsStringAsync());
            }

            var conteudo = await resposta.Content.ReadAsByteArrayAsync();
            var tipo = resposta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var nome = resposta.Content.Headers.ContentDisposition?.FileNameStar
                       ?? resposta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "wishbound-" + conjunto + "." + formato;

            return (conteudo, tipo, nome, null);
        }
    }
}
