using System.ComponentModel.DataAnnotations;

namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// Modelo "espelho" da entidade Personagem da WebAPI,
    /// com validação de dados de entrada (Data Annotations)
    /// usada nos formulários de criação/edição.
    /// </summary>
    public class Personagem
    {
        public int Id { get; set; }

        [Display(Name = "Nome")]
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 60 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Descrição")]
        [StringLength(500, ErrorMessage = "A descrição não pode ter mais de 500 caracteres.")]
        public string? Descricao { get; set; }

        [Display(Name = "Imagem (caminho ou URL)")]
        [StringLength(255, ErrorMessage = "O caminho da imagem não pode ter mais de 255 caracteres.")]
        public string? ImagemUrl { get; set; }

        [Display(Name = "Raridade")]
        [Range(1, int.MaxValue, ErrorMessage = "Escolha uma raridade.")]
        public int RaridadeId { get; set; }

        public Raridade? Raridade { get; set; }

        [Display(Name = "Data de criação")]
        public DateTime DataCriacao { get; set; }
    }
}
