namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs — GESTÃO DAS MOEDAS (api/admin/moedas).
    //  Os pedidos de escrita trazem sempre o AdminId e um Motivo
    //  opcional, que fica no registo de ações (LogsAdministrador).
    // ============================================================

    public class AdminTipoMoeda
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal EmCirculacao { get; set; }
        public int ContasComSaldo { get; set; }
        public decimal Ganho30Dias { get; set; }
        public decimal Gasto30Dias { get; set; }
        public int Transacoes { get; set; }

        /// <summary>Gemas, Moedas e Bilhetes: usadas pelo código, não se apagam.</summary>
        public bool Protegida { get; set; }

        public bool PodeApagar { get; set; }
    }

    public class AdminOrigemMoeda
    {
        public string Moeda { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int Movimentos { get; set; }
    }

    public class AdminMoedasResposta
    {
        public List<AdminTipoMoeda> Tipos { get; set; } = new List<AdminTipoMoeda>();
        public List<AdminOrigemMoeda> OrigensGanho { get; set; } = new List<AdminOrigemMoeda>();
        public List<AdminOrigemMoeda> OrigensGasto { get; set; } = new List<AdminOrigemMoeda>();
        public int ContasAtivas { get; set; }
    }

    public class AdminTipoMoedaPedido
    {
        public int AdminId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Motivo { get; set; }
    }

    public class AdminOfertaPedido
    {
        public int AdminId { get; set; }
        public int TipoMoedaId { get; set; }
        public decimal Quantidade { get; set; }

        /// <summary>Também avisa cada conta com uma notificação "Recompensa".</summary>
        public bool Notificar { get; set; }

        public string? Motivo { get; set; }
    }
}
