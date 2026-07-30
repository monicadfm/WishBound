using System.ComponentModel.DataAnnotations;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Personagem colecionável da plataforma WishBound.
    /// Validação de dados de entrada feita com Data Annotations.
    /// </summary>
    public class Personagem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 60 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "A descrição não pode ter mais de 500 caracteres.")]
        public string? Descricao { get; set; }

        [StringLength(300, ErrorMessage = "O caminho da imagem não pode ter mais de 300 caracteres.")]
        public string? ImagemUrl { get; set; }

        [Required(ErrorMessage = "A raridade é obrigatória.")]
        public int RaridadeId { get; set; }

        public Raridade? Raridade { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}
