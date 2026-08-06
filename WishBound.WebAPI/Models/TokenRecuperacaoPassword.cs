using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Token de uso único enviado ao utilizador por email.
    /// Mapeado para a tabela [TokensRecuperacaoPassword] da base de dados final.
    ///
    /// A tabela é usada para DOIS tipos de token (é a única tabela de tokens
    /// da base de dados), distinguidos pelo prefixo do valor:
    ///   "EV." - validação de email (expira em 24 horas);
    ///   "RP." - recuperação de password (expira em 1 hora).
    /// Cada token só pode ser usado uma vez (campo Utilizado).
    /// </summary>
    [Table("TokensRecuperacaoPassword")]
    public class TokenRecuperacaoPassword
    {
        [Key]
        [Column("TokenId")]
        public int Id { get; set; }

        public int UtilizadorId { get; set; }

        public Utilizador? Utilizador { get; set; }

        [Required]
        [StringLength(255)]
        public string Token { get; set; } = string.Empty;

        public DateTime DataExpiracao { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public bool Utilizado { get; set; }
    }
}
