namespace WishBound.ClientAPI.Models.Gestao
{
    // ============================================================
    //  Modelos da gestão — PAINEL (api/admin/painel). BannerAdmin e SerieDia também são
    //  usados pelas páginas de banners, notificações e estatísticas.
    //  Espelhos dos DTOs da WebAPI.
    // ============================================================

    public class BannerAdmin
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string TipoBanner { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public bool IsAtivo { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool EhPermanente { get; set; }
        public long SegundosRestantes { get; set; }
        public int Personagens { get; set; }
        public int RateUp { get; set; }
        public int Recompensas { get; set; }
        public int Invocacoes { get; set; }
        public int Participantes { get; set; }
        public bool PodeApagar { get; set; }

        public bool EhEvento => TipoBanner == "Evento";

        /// <summary>Classe CSS da etiqueta de estado.</summary>
        public string ClasseEstado => Estado switch
        {
            "A decorrer" => "ok",
            "Agendado" => "google",
            "Terminado" => "aviso",
            _ => "inativo"
        };

        /// <summary>Datas em hora local, para mostrar.</summary>
        public string Periodo => DataFim.Year >= 9000
            ? "desde " + Local(DataInicio).ToString("dd/MM/yyyy")
            : Local(DataInicio).ToString("dd/MM/yyyy HH:mm") + " – " + Local(DataFim).ToString("dd/MM/yyyy HH:mm");

        /// <summary>ISO UTC para o contador (data-fim do _Layout).</summary>
        public string FimIso => DateTime.SpecifyKind(DataFim, DateTimeKind.Utc).ToString("o");

        public static DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
    }

    public class AlertaAdmin
    {
        public string Nivel { get; set; } = "info";
        public string Texto { get; set; } = string.Empty;
        public string? Destino { get; set; }
        public int? DestinoId { get; set; }
    }

    public class SerieDia
    {
        public DateTime Data { get; set; }
        public int Total { get; set; }
    }

    public class PainelViewModel
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
        public List<BannerAdmin> Banners { get; set; } = new List<BannerAdmin>();
        public List<AcaoAdmin> UltimasAcoes { get; set; } = new List<AcaoAdmin>();
        public List<AlertaAdmin> Alertas { get; set; } = new List<AlertaAdmin>();
        public List<SerieDia> InvocacoesUltimos14Dias { get; set; } = new List<SerieDia>();
    }
}
