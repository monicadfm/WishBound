using System.Net;
using System.Net.Http.Json;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Services
{
    /// <summary>
    /// Pedidos do CRUD de BANNERS E EVENTOS (api/admin/banners): dados,
    /// pool + rate-up e recompensas diárias.
    /// Todos os pedidos levam o Id do administrador com sessão iniciada; a
    /// API confirma que é uma conta de administrador ativa.
    /// </summary>
    public partial class WishBoundApiService
    {
        // ---------- Banners / eventos ----------

        public async Task<List<BannerAdmin>> AdminObterBannersAsync(int adminId)
        {
            return await _http.GetFromJsonAsync<List<BannerAdmin>>("api/admin/banners?adminId=" + adminId)
                   ?? new List<BannerAdmin>();
        }

        public async Task<BannerDetalheViewModel?> AdminObterBannerAsync(int adminId, int id)
        {
            var resposta = await _http.GetAsync("api/admin/banners/" + id + "?adminId=" + adminId);

            if (resposta.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            resposta.EnsureSuccessStatusCode();
            return await resposta.Content.ReadFromJsonAsync<BannerDetalheViewModel>();
        }

        /// <summary>Cria (id null) ou edita um banner. As datas vão em UTC.</summary>
        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminGuardarBannerAsync(int adminId, int? id, BannerFormViewModel form)
        {
            var corpo = new
            {
                AdminId = adminId,
                form.Nome,
                form.Descricao,
                form.TipoBanner,
                form.ImagemUrl,
                DataInicio = form.InicioUtc,
                DataFim = form.FimUtc,
                form.IsAtivo,
                form.CopiarPoolDe,
                form.Notificar,
                form.Motivo
            };

            return id.HasValue
                ? EnviarAdminAsync(HttpMethod.Put, "api/admin/banners/" + id.Value, corpo)
                : EnviarAdminAsync(HttpMethod.Post, "api/admin/banners", corpo);
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminApagarBannerAsync(int adminId, int id, string? motivo)
        {
            return EnviarAdminAsync(HttpMethod.Delete, "api/admin/banners/" + id + "?adminId=" + adminId + Motivo(motivo), null);
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminGuardarPoolAsync(
            int adminId, int bannerId, IEnumerable<int> personagens, IEnumerable<int> rateUp, Dictionary<int, decimal> quotas, string? motivo)
        {
            var destacadas = rateUp.ToHashSet();
            var corpo = new
            {
                AdminId = adminId,
                Itens = personagens.Distinct().Select(p => new { PersonagemId = p, RateUp = destacadas.Contains(p) }).ToList(),
                QuotasPorRaridade = quotas,
                Motivo = motivo
            };

            return EnviarAdminAsync(HttpMethod.Post, "api/admin/banners/" + bannerId + "/pool", corpo);
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminGuardarDiaEventoAsync(
            int adminId, int bannerId, int? recompensaId, int tipoMoedaId, decimal quantidade, string? descricao, string? motivo)
        {
            var corpo = new { AdminId = adminId, TipoMoedaId = tipoMoedaId, Quantidade = quantidade, Descricao = descricao, Motivo = motivo };

            return recompensaId.HasValue
                ? EnviarAdminAsync(HttpMethod.Put, "api/admin/banners/" + bannerId + "/recompensas/" + recompensaId.Value, corpo)
                : EnviarAdminAsync(HttpMethod.Post, "api/admin/banners/" + bannerId + "/recompensas", corpo);
        }

        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminApagarDiaEventoAsync(int adminId, int bannerId, int recompensaId, string? motivo)
        {
            return EnviarAdminAsync(HttpMethod.Delete,
                "api/admin/banners/" + bannerId + "/recompensas/" + recompensaId + "?adminId=" + adminId + Motivo(motivo), null);
        }
    }
}
