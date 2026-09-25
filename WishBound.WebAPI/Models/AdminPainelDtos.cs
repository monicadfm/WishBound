namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs — PAINEL DA ADMINISTRAÇÃO (api/admin/painel) — e o resumo de banner
    //  e a série diária que as outras páginas de gestão também usam.
    //  Os pedidos de escrita trazem sempre o AdminId e um Motivo
    //  opcional, que fica no registo de ações (LogsAdministrador).
    // ============================================================

    public class AdminBannerResumo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string TipoBanner { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public bool IsAtivo { get; set; }

        /// <summary>"A decorrer", "Agendado", "Terminado" ou "Inativo".</summary>
        public string Estado { get; set; } = string.Empty;

        /// <summary>O banner permanente (Id 1) não pode ser desativado nem mudar de tipo.</summary>
        public bool EhPermanente { get; set; }

        public long SegundosRestantes { get; set; }
        public int Personagens { get; set; }
        public int RateUp { get; set; }
        public int Recompensas { get; set; }
        public int Invocacoes { get; set; }
        public int Participantes { get; set; }

        /// <summary>Só se apaga um banner sem invocações, pity nem participações.</summary>
        public bool PodeApagar { get; set; }
    }

    public class AdminAlerta
    {
        /// <summary>"aviso" ou "info".</summary>
        public string Nivel { get; set; } = "info";
        public string Texto { get; set; } = string.Empty;

        /// <summary>Página do site que resolve o alerta (Raridades, Banners, ...).</summary>
        public string? Destino { get; set; }
        public int? DestinoId { get; set; }
    }

    public class AdminSerieDia
    {
        public DateTime Data { get; set; }
        public int Total { get; set; }
    }

    public class AdminPainelResposta
    {
        public int ContasTotal { get; set; }
        public int ContasAtivas { get; set; }
        public int NovasContas7Dias { get; set; }
        public int ComLogin7Dias { get; set; }
        public int InvocacoesHoje { get; set; }
        public int Invocacoes7Dias { get; set; }
        public decimal MoedasEmCirculacao { get; set; }
        public decimal BilhetesEmCirculacao { get; set; }
        public int Personagens { get; set; }
        public int NotificacoesNaoLidas { get; set; }
        public List<AdminBannerResumo> Banners { get; set; } = new List<AdminBannerResumo>();
        public List<AdminAcaoResposta> UltimasAcoes { get; set; } = new List<AdminAcaoResposta>();
        public List<AdminAlerta> Alertas { get; set; } = new List<AdminAlerta>();
        public List<AdminSerieDia> InvocacoesUltimos14Dias { get; set; } = new List<AdminSerieDia>();
    }
}
