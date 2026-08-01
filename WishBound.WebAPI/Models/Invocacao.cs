using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Registo de uma invocação (obtenção aleatória de uma personagem).
    /// Mapeada para a tabela [HistoricoInvocacoes] da base de dados final.
    /// A base de dados final regista também QUEM invocou (UtilizadorId) e em
    /// QUE banner (BannerId). Enquanto não existir autenticação, a API usa o
    /// utilizador "Sistema" (Id 1) e o "Banner Permanente" (Id 1) criados
    /// pelo script de migração.
    /// </summary>
    [Table("HistoricoInvocacoes")]
    public class Invocacao
    {
        [Key]
        [Column("InvocacaoId")]
        public int Id { get; set; }

        public int UtilizadorId { get; set; }

        public int BannerId { get; set; }

        public int PersonagemId { get; set; }

        public Personagem? Personagem { get; set; }

        public int RaridadeId { get; set; }

        // true quando a invocação foi garantida pelo sistema de pity
        public bool PityAtivado { get; set; }

        [Column("DataInvocacao")]
        public DateTime Data { get; set; } = DateTime.UtcNow;
    }
}
