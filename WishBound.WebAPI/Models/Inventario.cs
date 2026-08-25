using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Espaço de coleção de um utilizador (tabela [InventarioUtilizador],
    /// uma linha por utilizador, criada no registo).
    ///
    /// A capacidade total é CapacidadeBase + CapacidadeExtra: a base é a que
    /// vem com a conta (100) e a extra é a comprada com moeda (funcionalidade
    /// da economia, ainda por implementar). Cada CÓPIA de personagem ocupa um
    /// lugar, por isso as repetidas também contam.
    /// </summary>
    [Table("InventarioUtilizador")]
    public class Inventario
    {
        // A coluna NÃO é IDENTITY (é a chave do utilizador, com FK para
        // Utilizadores): sem isto o EF assumiria que a base de dados gera o valor.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UtilizadorId { get; set; }

        public int CapacidadeBase { get; set; } = 100;

        public int CapacidadeExtra { get; set; }

        [NotMapped]
        public int CapacidadeTotal => CapacidadeBase + CapacidadeExtra;
    }
}
