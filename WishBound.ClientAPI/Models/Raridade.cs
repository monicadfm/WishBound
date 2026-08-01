namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// Modelo "espelho" da entidade Raridade da WebAPI.
    /// Na base de dados final a probabilidade é uma fração
    /// (ex.: 0.55 = 55%), por isso a propriedade passou a decimal.
    /// </summary>
    public class Raridade
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cor { get; set; } = "#9aa5b1";
        public decimal Probabilidade { get; set; }

        /// <summary>Probabilidade formatada para mostrar nas views (ex.: "55%").</summary>
        public string ProbabilidadePercentagem => (Probabilidade * 100m).ToString("0.##") + "%";
    }
}
