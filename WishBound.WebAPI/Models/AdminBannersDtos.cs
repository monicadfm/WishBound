namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs — CRUD DE BANNERS E EVENTOS (api/admin/banners): pool, rate-up e
    //  recompensas diárias. O resumo de cada banner (AdminBannerResumo)
    //  está em AdminPainelDtos.cs.
    //  Os pedidos de escrita trazem sempre o AdminId e um Motivo
    //  opcional, que fica no registo de ações (LogsAdministrador).
    // ============================================================

    public class AdminBannerPersonagem
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

        /// <summary>Está em algum banner Standard (se não estiver, é exclusiva de eventos).</summary>
        public bool EmStandard { get; set; }
    }

    public class AdminRecompensaEvento
    {
        public int Id { get; set; }
        public int Dia { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public int? TipoMoedaId { get; set; }
        public string? MoedaNome { get; set; }
        public decimal? Quantidade { get; set; }

        /// <summary>Participantes que já receberam este dia.</summary>
        public int JaRecebido { get; set; }
    }

    public class AdminOpcao
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public int Ordem { get; set; }
    }

    public class AdminBannerDetalhe
    {
        public AdminBannerResumo Banner { get; set; } = new AdminBannerResumo();
        public List<AdminBannerPersonagem> Pool { get; set; } = new List<AdminBannerPersonagem>();
        public List<AdminRecompensaEvento> RecompensasEvento { get; set; } = new List<AdminRecompensaEvento>();
        public List<AdminOpcao> TiposMoeda { get; set; } = new List<AdminOpcao>();
        public List<AdminOpcao> Raridades { get; set; } = new List<AdminOpcao>();

        /// <summary>Quota do rate-up atual por raridade (RaridadeId → fração).</summary>
        public Dictionary<int, decimal> QuotasPorRaridade { get; set; } = new Dictionary<int, decimal>();

        /// <summary>Outros banners (para "copiar a pool de").</summary>
        public List<AdminOpcao> OutrosBanners { get; set; } = new List<AdminOpcao>();
    }

    public class AdminBannerPedido
    {
        public int AdminId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string TipoBanner { get; set; } = Banner.TipoEvento;
        public string? ImagemUrl { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public bool IsAtivo { get; set; } = true;

        /// <summary>Só na criação: copia as personagens (e o rate-up) de outro banner.</summary>
        public int? CopiarPoolDe { get; set; }

        /// <summary>Envia uma notificação ("Evento"/"Banner") a todas as contas ativas.</summary>
        public bool Notificar { get; set; }

        public string? Motivo { get; set; }
    }

    public class AdminPoolItem
    {
        public int PersonagemId { get; set; }
        public bool RateUp { get; set; }
    }

    public class AdminBannerPoolPedido
    {
        public int AdminId { get; set; }
        public List<AdminPoolItem> Itens { get; set; } = new List<AdminPoolItem>();

        /// <summary>Quota do rate-up por raridade (RaridadeId → fração 0.01..0.99).</summary>
        public Dictionary<int, decimal> QuotasPorRaridade { get; set; } = new Dictionary<int, decimal>();

        public string? Motivo { get; set; }
    }

    public class AdminRecompensaEventoPedido
    {
        public int AdminId { get; set; }
        public string? Descricao { get; set; }
        public int TipoMoedaId { get; set; }
        public decimal Quantidade { get; set; }
        public string? Motivo { get; set; }
    }
}
