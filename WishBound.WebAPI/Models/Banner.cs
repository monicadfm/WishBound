using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Banner de invocação (tabela [Banners]). O "Banner Permanente" (Id 1) é
    /// criado pela migração e tem todas as personagens; os banners de evento
    /// são temporários (DataInicio/DataFim) e podem ter só algumas.
    ///
    /// A base de dados só aceita "Standard" ou "Evento" em TipoBanner (CHECK).
    /// </summary>
    [Table("Banners")]
    public class Banner
    {
        public const string TipoStandard = "Standard";
        public const string TipoEvento = "Evento";

        [Key]
        [Column("BannerId")]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        [StringLength(20)]
        public string TipoBanner { get; set; } = TipoStandard;

        [StringLength(255)]
        public string? ImagemUrl { get; set; }

        public DateTime DataInicio { get; set; }

        public DateTime DataFim { get; set; }

        public bool IsAtivo { get; set; } = true;

        /// <summary>Está a decorrer agora? (ativo e dentro das datas)</summary>
        [NotMapped]
        public bool ADecorrer => IsAtivo && DataInicio <= DateTime.UtcNow && DataFim >= DateTime.UtcNow;
    }

    /// <summary>
    /// Personagens que fazem parte de um banner (tabela [BannerPersonagens],
    /// chave composta banner + personagem, declarada no WishBoundContext).
    /// RateUp/ProbabilidadeExtra ficam para a funcionalidade de eventos.
    /// </summary>
    [Table("BannerPersonagens")]
    public class BannerPersonagem
    {
        public int BannerId { get; set; }

        public int PersonagemId { get; set; }

        public bool RateUp { get; set; }

        [Column(TypeName = "decimal(6,4)")]
        public decimal? ProbabilidadeExtra { get; set; }
    }

    /// <summary>
    /// Contadores de pity de um utilizador NUM banner (tabela [PityUtilizador],
    /// chave composta utilizador + banner).
    ///
    ///   ContadorAtual  - invocações desde a última Lendária ou Mítica.
    ///                    Aos 90 a raridade alta é garantida.
    ///   ContadorEpico  - invocações desde a última Épica ou melhor.
    ///                    Aos 10 a Épica é garantida (coluna acrescentada
    ///                    pelo script Database/Migracao02.sql).
    ///   UltimaRaridadeGarantida - raridade da última invocação que saiu por
    ///                    garantia (para o histórico/estatísticas).
    /// </summary>
    [Table("PityUtilizador")]
    public class Pity
    {
        public int UtilizadorId { get; set; }

        public int BannerId { get; set; }

        public int ContadorAtual { get; set; }

        public int ContadorEpico { get; set; }

        public int? UltimaRaridadeGarantida { get; set; }
    }
}
