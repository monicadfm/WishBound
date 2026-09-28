namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs — GESTÃO DAS NOTIFICAÇÕES (api/admin/notificacoes).
    //  Os pedidos de escrita trazem sempre o AdminId e um Motivo
    //  opcional, que fica no registo de ações (LogsAdministrador).
    // ============================================================

    public class AdminNotificacaoLinha
    {
        public int Id { get; set; }
        public int UtilizadorId { get; set; }
        public string UtilizadorNome { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public bool IsLida { get; set; }
        public DateTime Data { get; set; }
    }

    public class AdminContagemTipo
    {
        public string Tipo { get; set; } = string.Empty;
        public int Total { get; set; }
        public int NaoLidas { get; set; }
    }

    public class AdminNotificacoesResposta
    {
        public int Total { get; set; }
        public int NaoLidas { get; set; }
        public int ContasAtivas { get; set; }
        public List<AdminContagemTipo> PorTipo { get; set; } = new List<AdminContagemTipo>();
        public List<AdminNotificacaoLinha> Recentes { get; set; } = new List<AdminNotificacaoLinha>();
        public List<AdminAcaoResposta> Envios { get; set; } = new List<AdminAcaoResposta>();

        /// <summary>Tipos que um administrador pode enviar (os do CHECK, menos o das personagens).</summary>
        public List<string> TiposPermitidos { get; set; } = new List<string>();
    }

    public class AdminNotificacaoPedido
    {
        public int AdminId { get; set; }
        public string Tipo { get; set; } = "Evento";
        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;

        /// <summary>null = todas as contas ativas.</summary>
        public int? UtilizadorId { get; set; }

        public string? Motivo { get; set; }
    }

    public class AdminLembretesPedido
    {
        public int AdminId { get; set; }
    }

    public class AdminLimparNotificacoesPedido
    {
        public int AdminId { get; set; }

        /// <summary>Apaga as notificações com mais de N dias.</summary>
        public int Dias { get; set; } = 30;

        /// <summary>true = só as já lidas.</summary>
        public bool ApenasLidas { get; set; } = true;

        public string? Motivo { get; set; }
    }
}
