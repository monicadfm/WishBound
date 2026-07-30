using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Raridade de uma personagem (Comum, Raro, Épico, Lendário, Mítico).
    /// A propriedade Probabilidade define o "peso" usado no sistema de invocação.
    /// </summary>
    public class Raridade
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da raridade é obrigatório.")]
        [StringLength(30, ErrorMessage = "O nome não pode ter mais de 30 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(9)]
        public string Cor { get; set; } = "#9aa5b1";

        [Range(1, 100, ErrorMessage = "A probabilidade deve estar entre 1 e 100.")]
        public int Probabilidade { get; set; }

        // Relação 1:N - uma raridade tem várias personagens
        [JsonIgnore]
        public ICollection<Personagem>? Personagens { get; set; }
    }
}
