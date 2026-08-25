using System.ComponentModel.DataAnnotations;

namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs da coleção pessoal (o que a API devolve ao site).
    // ============================================================

    /// <summary>Uma personagem da coleção, já com os dados da raridade.</summary>
    public class ItemColecaoResposta
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemUrl { get; set; }

        public int RaridadeId { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public int RaridadeOrdem { get; set; }

        public int Quantidade { get; set; }
        public bool IsFavorito { get; set; }
        public DateTime DataObtencao { get; set; }
    }

    /// <summary>
    /// A coleção completa de um utilizador: as personagens e o resumo do
    /// espaço ocupado (para a barra de capacidade do site).
    /// </summary>
    public class ColecaoResposta
    {
        public List<ItemColecaoResposta> Itens { get; set; } = new List<ItemColecaoResposta>();

        /// <summary>Cópias que o utilizador tem (soma das quantidades).</summary>
        public int Ocupado { get; set; }

        /// <summary>CapacidadeBase + CapacidadeExtra.</summary>
        public int Capacidade { get; set; }

        /// <summary>Personagens diferentes que já obteve.</summary>
        public int PersonagensDistintas { get; set; }

        /// <summary>Total de personagens ativas na plataforma (para "7 de 8").</summary>
        public int PersonagensExistentes { get; set; }
    }

    /// <summary>Marcar/desmarcar uma personagem como favorita.</summary>
    public class FavoritoPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        [Required]
        public int PersonagemId { get; set; }

        public bool Favorito { get; set; }
    }

    /// <summary>
    /// Libertar cópias repetidas de uma personagem (gestão de duplicados):
    /// devolve espaço à coleção mantendo sempre pelo menos uma cópia.
    /// </summary>
    public class LibertarPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        [Required]
        public int PersonagemId { get; set; }

        /// <summary>Quantas cópias libertar (0 ou menos = todas as repetidas).</summary>
        public int Quantidade { get; set; } = 1;
    }
}
