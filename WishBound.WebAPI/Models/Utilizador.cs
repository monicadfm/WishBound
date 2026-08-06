using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Utilizador da plataforma WishBound.
    /// Mapeado para a tabela [Utilizadores] da base de dados final
    /// (a propriedade Id corresponde à coluna UtilizadorId).
    ///
    /// A password NUNCA é guardada em texto simples: apenas o hash
    /// (ver Services/PasswordHasher.cs). O campo é nullable porque as
    /// contas criadas com Google (futuro) não têm password local.
    /// </summary>
    [Table("Utilizadores")]
    public class Utilizador
    {
        [Key]
        [Column("UtilizadorId")]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome de utilizador deve ter entre 3 e 50 caracteres.")]
        public string NomeUtilizador { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email não é válido.")]
        [StringLength(150, ErrorMessage = "O email não pode ter mais de 150 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(255)]
        public string? PasswordHash { get; set; }

        // Identificador da conta Google (login com Google - funcionalidade opcional futura)
        [StringLength(100)]
        public string? GoogleId { get; set; }

        // Passa a true quando o utilizador confirma o email através do link de validação
        public bool EmailValidado { get; set; }

        [StringLength(255)]
        public string? FotoPerfilUrl { get; set; }

        // Moldura de perfil escolhida (sistema de amizade - futuro)
        public int? MolduraPerfilAtualId { get; set; }

        // Apenas os administradores acedem à área de gestão
        public bool IsAdmin { get; set; }

        // Permite desativar contas sem as apagar
        public bool IsAtivo { get; set; } = true;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Último login efetuado (qualquer hora)
        public DateTime? UltimoLogin { get; set; }

        // Último dia em que recebeu a recompensa de login diário (economia - futuro).
        // DateOnly porque a coluna na base de dados é do tipo "date" (sem horas).
        public DateOnly? UltimoLoginDiario { get; set; }
    }
}
