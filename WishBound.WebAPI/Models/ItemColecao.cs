using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Uma linha da coleção de um utilizador: a personagem que possui, quantas
    /// cópias tem e se está marcada como favorita.
    /// Mapeada para a tabela [ColecaoUtilizador] (chave ColecaoId; a base de
    /// dados garante UNIQUE(UtilizadorId, PersonagemId) — cada personagem
    /// aparece uma única vez por utilizador, com um contador de cópias).
    ///
    /// Os campos de amizade (PontosAmizade / NivelAmizadeId / UltimaInteracao)
    /// já existem na tabela mas só serão usados no sistema de amizade; aqui
    /// ficam com os valores iniciais (0 pontos, nível 1 "Desconhecido").
    /// </summary>
    [Table("ColecaoUtilizador")]
    public class ItemColecao
    {
        [Key]
        [Column("ColecaoId")]
        public int Id { get; set; }

        public int UtilizadorId { get; set; }

        public int PersonagemId { get; set; }

        public Personagem? Personagem { get; set; }

        /// <summary>Número de cópias obtidas (a primeira mais as repetidas).</summary>
        public int Quantidade { get; set; } = 1;

        public bool IsFavorito { get; set; }

        public int PontosAmizade { get; set; }

        /// <summary>1 = "Desconhecido", o primeiro nível de amizade.</summary>
        public int NivelAmizadeId { get; set; } = 1;

        public DateTime DataObtencao { get; set; } = DateTime.UtcNow;

        /// <summary>Coluna do tipo "date" na base de dados (sistema de amizade).</summary>
        public DateOnly? UltimaInteracao { get; set; }
    }
}
