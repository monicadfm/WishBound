using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Registo de uma invocação (obtenção aleatória de uma personagem).
    /// Mapeada para a tabela [HistoricoInvocacoes] da base de dados final.
    /// Regista QUEM invocou (UtilizadorId — o utilizador autenticado, enviado
    /// pelo site) e em QUE banner (BannerId — por agora sempre o "Banner
    /// Permanente" Id 1; a escolha de banner chega com os eventos).
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
