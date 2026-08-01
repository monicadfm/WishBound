using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Raridade de uma personagem (Comum, Raro, Épico, Lendário, Mítico).
    /// Mapeada para a tabela [Raridades] da base de dados final WishBound.
    /// A Probabilidade é agora uma fração (ex.: 0.55 = 55%) usada como "peso"
    /// no sistema de invocação. Os nomes das propriedades mantêm-se iguais
    /// aos da versão mini para o JSON da API não mudar (o ClientAPI continua
    /// a funcionar); os atributos [Column] fazem a ponte para as colunas reais.
    /// </summary>
    [Table("Raridades")]
    public class Raridade
    {
        [Key]
        [Column("RaridadeId")]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da raridade é obrigatório.")]
        [StringLength(30, ErrorMessage = "O nome não pode ter mais de 30 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Column("CorHex")]
        [StringLength(7)]
        public string? Cor { get; set; } = "#9aa5b1";

        [Column(TypeName = "decimal(6,4)")]
        [Range(0.0001, 1.0, ErrorMessage = "A probabilidade deve ser uma fração entre 0 e 1 (ex.: 0.55).")]
        public decimal Probabilidade { get; set; }

        // Posição na hierarquia de raridades (1 = Comum ... 5 = Mítico)
        public int Ordem { get; set; }

        // Relação 1:N - uma raridade tem várias personagens
        [JsonIgnore]
        public ICollection<Personagem>? Personagens { get; set; }
    }
}
