namespace WishBound.ClientAPI.Models.Gestao
{
    // ============================================================
    //  Modelos da gestão — NOTIFICAÇÕES (api/admin/notificacoes).
    //  Espelhos dos DTOs da WebAPI.
    // ============================================================

    public class NotificacaoAdminLinha
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

    public class ContagemTipoAdmin
    {
        public string Tipo { get; set; } = string.Empty;
        public int Total { get; set; }
        public int NaoLidas { get; set; }
    }

    public class NotificacoesAdminViewModel
    {
        public int Total { get; set; }
        public int NaoLidas { get; set; }
        public int ContasAtivas { get; set; }
        public List<ContagemTipoAdmin> PorTipo { get; set; } = new List<ContagemTipoAdmin>();
        public List<NotificacaoAdminLinha> Recentes { get; set; } = new List<NotificacaoAdminLinha>();
        public List<AcaoAdmin> Envios { get; set; } = new List<AcaoAdmin>();
        public List<string> TiposPermitidos { get; set; } = new List<string> { "Evento", "Banner", "Recompensa", "LoginDiario" };

        /// <summary>Conta pré-escolhida (vinda do link "Notificar" na página de uma conta).</summary>
        public int? UtilizadorId { get; set; }
        public string? UtilizadorNome { get; set; }
    }
}
