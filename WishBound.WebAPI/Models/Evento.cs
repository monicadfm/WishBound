using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Recompensa de um evento (tabela [RecompensasEvento]). Um evento é um
    /// banner de tipo "Evento"; cada linha desta tabela é UM DIA do evento
    /// (a ordem das linhas é a ordem dos dias, MetaNecessaria guarda o dia).
    ///
    /// Por agora as recompensas são em moeda (TipoMoedaId + QuantidadeMoeda,
    /// normalmente Bilhetes). O PersonagemId fica preparado para recompensas
    /// de personagem no futuro.
    /// </summary>
    [Table("RecompensasEvento")]
    public class RecompensaEvento
    {
        [Key]
        [Column("RecompensaId")]
        public int Id { get; set; }

        public int BannerId { get; set; }

        [StringLength(255)]
        public string Descricao { get; set; } = string.Empty;

        public int? TipoMoedaId { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? QuantidadeMoeda { get; set; }

        public int? PersonagemId { get; set; }

        [StringLength(100)]
        public string? MetaNecessaria { get; set; }
    }

    /// <summary>
    /// Participação de um utilizador num evento (tabela [ParticipacaoEventos],
    /// UNIQUE utilizador + banner).
    ///
    ///   Progresso            - dias já recebidos (0..N);
    ///   DataParticipacao     - momento do último resgate (para só permitir um
    ///                          por dia);
    ///   RecompensasResgatadas - true quando recebeu todos os dias do evento.
    /// </summary>
    [Table("ParticipacaoEventos")]
    public class ParticipacaoEvento
    {
        [Key]
        [Column("ParticipacaoId")]
        public int Id { get; set; }

        public int UtilizadorId { get; set; }

        public int BannerId { get; set; }

        public int Progresso { get; set; }

        public bool RecompensasResgatadas { get; set; }

        public DateTime DataParticipacao { get; set; } = DateTime.UtcNow;
    }
}
