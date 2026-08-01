using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Personagem colecionável da plataforma WishBound.
    /// Mapeada para a tabela [Personagens] da base de dados final WishBound
    /// (a propriedade Id corresponde à coluna PersonagemId).
    /// Validação de dados de entrada feita com Data Annotations.
    /// </summary>
    [Table("Personagens")]
    public class Personagem
    {
        [Key]
        [Column("PersonagemId")]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 60 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "A descrição não pode ter mais de 500 caracteres.")]
        public string? Descricao { get; set; }

        [StringLength(255, ErrorMessage = "O caminho da imagem não pode ter mais de 255 caracteres.")]
        public string? ImagemUrl { get; set; }

        [Required(ErrorMessage = "A raridade é obrigatória.")]
        public int RaridadeId { get; set; }

        public Raridade? Raridade { get; set; }

        // Permite "desativar" uma personagem sem a apagar (soft delete futuro)
        public bool IsAtivo { get; set; } = true;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
