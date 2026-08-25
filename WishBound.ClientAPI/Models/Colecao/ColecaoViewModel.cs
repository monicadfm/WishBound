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
