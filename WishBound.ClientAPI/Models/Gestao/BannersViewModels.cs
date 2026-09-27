namespace WishBound.ClientAPI.Models.Gestao
{
    // ============================================================
    //  Modelos da gestão — BANNERS E EVENTOS (api/admin/banners).
    //  Espelhos dos DTOs da WebAPI.
    // ============================================================

    public class BannerPersonagemAdmin
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public int RaridadeId { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public int RaridadeOrdem { get; set; }
        public bool IsAtivo { get; set; }
        public bool NoBanner { get; set; }
        public bool RateUp { get; set; }
        public decimal? Quota { get; set; }
        public bool EmStandard { get; set; }
    }

    public class RecompensaEventoAdmin
    {
        public int Id { get; set; }
        public int Dia { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public int? TipoMoedaId { get; set; }
        public string? MoedaNome { get; set; }
        public decimal? Quantidade { get; set; }
        public int JaRecebido { get; set; }

        public string QuantidadeFormulario =>
            (Quantidade ?? 0m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    public class OpcaoAdmin
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public int Ordem { get; set; }
    }

    public class BannerDetalheViewModel
    {
        public BannerAdmin Banner { get; set; } = new BannerAdmin();
        public List<BannerPersonagemAdmin> Pool { get; set; } = new List<BannerPersonagemAdmin>();
        public List<RecompensaEventoAdmin> RecompensasEvento { get; set; } = new List<RecompensaEventoAdmin>();
        public List<OpcaoAdmin> TiposMoeda { get; set; } = new List<OpcaoAdmin>();
        public List<OpcaoAdmin> Raridades { get; set; } = new List<OpcaoAdmin>();
        public Dictionary<int, decimal> QuotasPorRaridade { get; set; } = new Dictionary<int, decimal>();
        public List<OpcaoAdmin> OutrosBanners { get; set; } = new List<OpcaoAdmin>();

        /// <summary>Quota em % para o formulário (80 por omissão nas Lendárias, 50 nas Míticas).</summary>
        public int QuotaPercentagem(OpcaoAdmin raridade) =>
            QuotasPorRaridade.TryGetValue(raridade.Id, out var q)
                ? (int)Math.Round(q * 100m)
                : (raridade.Ordem >= 5 ? 50 : 80);
    }

    /// <summary>
    /// Formulário de criar/editar banner. As datas vêm dos campos
    /// datetime-local ("2026-09-25T18:00", hora de Portugal) como texto, para
    /// não dependerem da cultura do servidor; InicioUtc/FimUtc convertem.
    /// </summary>
    public class BannerFormViewModel
    {
        public int? Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string TipoBanner { get; set; } = "Evento";
        public string? ImagemUrl { get; set; }
        public string? Inicio { get; set; }
        public string? Fim { get; set; }
        /// <summary>Checkbox: só chega "true" quando está marcada (por isso o valor por omissão é false).</summary>
        public bool IsAtivo { get; set; }
        public int? CopiarPoolDe { get; set; }
        public bool Notificar { get; set; }
        public string? Motivo { get; set; }

        public DateTime? InicioUtc => ParaUtc(Inicio);
        public DateTime? FimUtc => ParaUtc(Fim);

        /// <summary>Valor para um input datetime-local a partir de uma data UTC.</summary>
        public static string ParaCampo(DateTime utc) =>
            DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);

        private static DateTime? ParaUtc(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            string[] formatos = { "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(texto.Trim(), formatos, System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.AssumeLocal, out var local))
            {
                return local.ToUniversalTime();
            }

            return null;
        }
    }

    public class BannersViewModel
    {
        public List<BannerAdmin> Itens { get; set; } = new List<BannerAdmin>();

        /// <summary>Formulário "Novo banner" (valores por omissão: evento de 14 dias a começar agora).</summary>
        public BannerFormViewModel Form { get; set; } = new BannerFormViewModel
        {
            Inicio = BannerFormViewModel.ParaCampo(DateTime.UtcNow),
            Fim = BannerFormViewModel.ParaCampo(DateTime.UtcNow.AddDays(14)),
            CopiarPoolDe = 1
        };
    }
}
