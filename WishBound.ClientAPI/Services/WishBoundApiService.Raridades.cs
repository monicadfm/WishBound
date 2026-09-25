using System.Net.Http.Json;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Services
{
    /// <summary>
    /// Pedidos do CRUD das RARIDADES (api/admin/raridades).
    /// Todos os pedidos levam o Id do administrador com sessão iniciada; a
    /// API confirma que é uma conta de administrador ativa.
    /// </summary>
    public partial class WishBoundApiService
    {
        // ---------- Raridades ----------

        public async Task<RaridadesViewModel> AdminObterRaridadesAsync(int adminId)
        {
            return await _http.GetFromJsonAsync<RaridadesViewModel>("api/admin/raridades?adminId=" + adminId)
                   ?? new RaridadesViewModel();
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminGuardarRaridadeAsync(
            int adminId, int? id, string nome, string cor, decimal probabilidade, int ordem, string? motivo)
        {
            var corpo = new { AdminId = adminId, Nome = nome, Cor = cor, Probabilidade = probabilidade, Ordem = ordem, Motivo = motivo };
            return id.HasValue
                ? EnviarAdminAsync(HttpMethod.Put, "api/admin/raridades/" + id.Value, corpo)
                : EnviarAdminAsync(HttpMethod.Post, "api/admin/raridades", corpo);
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminApagarRaridadeAsync(int adminId, int id, string? motivo)
        {
            return EnviarAdminAsync(HttpMethod.Delete, "api/admin/raridades/" + id + "?adminId=" + adminId + Motivo(motivo), null);
        }
    }
}
