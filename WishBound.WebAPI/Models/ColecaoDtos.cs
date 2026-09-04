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

        // ----- Amizade com esta personagem (sistema de amizade) -----
        public int PontosAmizade { get; set; }
        public int NivelAmizadeId { get; set; }
        public string NivelAmizadeNome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }

        /// <summary>Nível mais alto que esta personagem pode atingir (Comum 3 ... Mítico 7).</summary>
        public int NivelMaximoOrdem { get; set; }
        public string NivelMaximoNome { get; set; } = string.Empty;

        /// <summary>Pontos onde começa o nível atual (para a barra de progresso).</summary>
        public int PontosNivelAtual { get; set; }

        /// <summary>Pontos do próximo nível (null quando já está no máximo).</summary>
        public int? PontosProximoNivel { get; set; }

        public DateOnly? UltimaInteracao { get; set; }
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

        /// <summary>Saldo do utilizador em "Moedas" (a moeda normal).</summary>
        public decimal SaldoMoedas { get; set; }

        /// <summary>Quanto custa a próxima expansão do inventário.</summary>
        public decimal PrecoProximaExpansao { get; set; }

        /// <summary>Lugares que cada expansão acrescenta.</summary>
        public int LugaresPorExpansao { get; set; }

        /// <summary>Cópias repetidas que o utilizador tem ao todo.</summary>
        public int TotalRepetidas { get; set; }

        /// <summary>Moedas que ganharia se libertasse todas as repetidas.</summary>
        public decimal MoedasPorTodasRepetidas { get; set; }

        /// <summary>Interações do sistema de amizade que ainda tem hoje (3 por dia).</summary>
        public int InteracoesRestantes { get; set; }

        public int InteracoesPorDia { get; set; }
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

    /// <summary>Comprar mais lugares para a coleção, pagando em Moedas.</summary>
    public class ExpandirPedido
    {
        [Required]
        public int UtilizadorId { get; set; }
    }
}
