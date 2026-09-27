using System.Net.Http.Json;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Services
{
    /// <summary>
    /// Pedidos da GESTÃO DAS MOEDAS (api/admin/moedas).
    /// Todos os pedidos levam o Id do administrador com sessão iniciada; a
    /// API confirma que é uma conta de administrador ativa.
    /// </summary>
    public partial class WishBoundApiService
    {
        // ---------- Moedas ----------

        public async Task<MoedasViewModel> AdminObterMoedasAsync(int adminId)
        {
            return await _http.GetFromJsonAsync<MoedasViewModel>("api/admin/moedas?adminId=" + adminId)
                   ?? new MoedasViewModel();
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminGuardarTipoMoedaAsync(int adminId, int? id, string nome, string? motivo)
        {
            var corpo = new { AdminId = adminId, Nome = nome, Motivo = motivo };
            return id.HasValue
                ? EnviarAdminAsync(HttpMethod.Put, "api/admin/moedas/tipos/" + id.Value, corpo)
                : EnviarAdminAsync(HttpMethod.Post, "api/admin/moedas/tipos", corpo);
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminApagarTipoMoedaAsync(int adminId, int id, string? motivo)
        {
            return EnviarAdminAsync(HttpMethod.Delete, "api/admin/moedas/tipos/" + id + "?adminId=" + adminId + Motivo(motivo), null);
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminOfertaAsync(
            int adminId, int tipoMoedaId, decimal quantidade, bool notificar, string? motivo)
        {
            return EnviarAdminAsync(HttpMethod.Post, "api/admin/moedas/oferta", new
            {
                AdminId = adminId,
                TipoMoedaId = tipoMoedaId,
                Quantidade = quantidade,
                Notificar = notificar,
                Motivo = motivo
            });
        }
    }
}
