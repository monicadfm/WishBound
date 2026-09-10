using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Mensagem de uma personagem (tabela [MensagensPersonagem], do esquema
    /// original; usada desde a Migracao07).
    ///
    /// Cada personagem tem CONJUNTOS de mensagens, desbloqueados pelo nível
    /// de amizade (NivelAmizadeId = nível a partir do qual a mensagem está
    /// disponível):
    ///   - "Saudacao"  — o que diz quando o utilizador abre os seus detalhes
    ///                   ou quando é a companheira da página inicial;
    ///   - "Aleatoria" — a reação a uma interação (caixa rosa);
    ///   - "Diaria"    — a mensagem deixada nas notificações na primeira
    ///                   interação de cada dia (só a partir do nível 4,
    ///                   Confidente — é a recompensa desse nível).
    /// O tipo está limitado a estes três valores por um CHECK na tabela.
    /// </summary>
    [Table("MensagensPersonagem")]
    public class MensagemPersonagem
    {
        public const string TipoSaudacao = "Saudacao";
        public const string TipoAleatoria = "Aleatoria";
        public const string TipoDiaria = "Diaria";

        public static readonly string[] Tipos = { TipoSaudacao, TipoAleatoria, TipoDiaria };

        [Key]
        [Column("MensagemId")]
        public int Id { get; set; }

        public int PersonagemId { get; set; }

        public Personagem? Personagem { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoMensagem { get; set; } = TipoSaudacao;

        /// <summary>Nível de amizade a partir do qual a mensagem está desbloqueada.</summary>
        public int NivelAmizadeId { get; set; }

        [Required]
        public string Conteudo { get; set; } = string.Empty;
    }
}
