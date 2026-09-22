using System.ComponentModel.DataAnnotations;

namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs das NOTIFICAÇÕES (api/notificacoes).
    //  Usados pela app móvel e, mais tarde, pela página de
    //  notificações do site (§9).
    // ============================================================

    /// <summary>Uma notificação do utilizador.</summary>
    public class NotificacaoResposta
    {
        public int Id { get; set; }

        /// <summary>'MensagemPersonagem', 'Banner', 'Evento', 'Recompensa' ou 'LoginDiario'.</summary>
        public string Tipo { get; set; } = string.Empty;

        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public bool IsLida { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    /// <summary>Lista de notificações + contagens.</summary>
    public class NotificacoesResposta
    {
        /// <summary>Notificações por ler (de todas, não só das devolvidas).</summary>
        public int NaoLidas { get; set; }

        /// <summary>Total de notificações do utilizador.</summary>
        public int Total { get; set; }

        public List<NotificacaoResposta> Itens { get; set; } = new List<NotificacaoResposta>();
    }

    /// <summary>Marcar como lida: uma notificação (NotificacaoId) ou todas (null).</summary>
    public class MarcarLidaPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        /// <summary>null = marcar todas as notificações do utilizador como lidas.</summary>
        public int? NotificacaoId { get; set; }
    }
}
