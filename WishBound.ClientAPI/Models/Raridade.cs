namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// Modelo "espelho" da entidade Raridade da WebAPI.
    /// </summary>
    public class Raridade
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cor { get; set; } = "#9aa5b1";
        public int Probabilidade { get; set; }
    }
}
