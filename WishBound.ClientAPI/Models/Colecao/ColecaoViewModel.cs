namespace WishBound.ClientAPI.Models.Colecao
{
    /// <summary>
    /// ViewModel da página "A minha coleção": as personagens obtidas, o
    /// espaço ocupado no inventário e as opções de ordenação/filtro.
    /// </summary>
    public class ColecaoViewModel
    {
        public List<ItemColecao> Itens { get; set; } = new List<ItemColecao>();

        /// <summary>Cópias que o utilizador tem (as repetidas também contam).</summary>
        public int Ocupado { get; set; }

        /// <summary>CapacidadeBase + CapacidadeExtra do inventário.</summary>
        public int Capacidade { get; set; }

        public int PersonagensDistintas { get; set; }

        public int PersonagensExistentes { get; set; }

        /// <summary>Saldo em "Moedas" (ganham-se a libertar cópias repetidas).</summary>
        public decimal SaldoMoedas { get; set; }

        /// <summary>Preço da próxima expansão do inventário.</summary>
        public decimal PrecoProximaExpansao { get; set; }

        /// <summary>Lugares que a expansão acrescenta (10).</summary>
        public int LugaresPorExpansao { get; set; }

        /// <summary>Cópias repetidas que tem ao todo.</summary>
        public int TotalRepetidas { get; set; }

        /// <summary>Moedas que ganharia se libertasse todas as repetidas.</summary>
        public decimal MoedasPorTodasRepetidas { get; set; }

        /// <summary>Interações do sistema de amizade que ainda tem hoje.</summary>
        public int InteracoesRestantes { get; set; }

        public int InteracoesPorDia { get; set; } = 3;

        public bool TemInteracoes => InteracoesRestantes > 0;

        /// <summary>Há repetidas para libertar de uma vez?</summary>
        public bool TemRepetidas => TotalRepetidas > 0;

        /// <summary>Já tem Moedas suficientes para comprar a próxima expansão?</summary>
        public bool PodeExpandir => PrecoProximaExpansao > 0 && SaldoMoedas >= PrecoProximaExpansao;

        /// <summary>Ordenação escolhida: "raridade" (omissão), "nome" ou "data".</summary>
        public string Ordenar { get; set; } = "raridade";

        public bool ApenasFavoritos { get; set; }

        /// <summary>Percentagem de espaço usado, para a barra de capacidade.</summary>
        public int PercentagemOcupada => Capacidade <= 0
            ? 0
            : (int)Math.Min(100, Math.Round(Ocupado * 100.0 / Capacidade));

        public bool Cheia => Capacidade > 0 && Ocupado >= Capacidade;
    }
}
