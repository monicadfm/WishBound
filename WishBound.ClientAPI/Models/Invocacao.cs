namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// Modelo "espelho" da entidade Invocacao da WebAPI (histórico do gacha).
    /// </summary>
    public class Invocacao
    {
        public int Id { get; set; }
        public int PersonagemId { get; set; }
        public Personagem? Personagem { get; set; }
        public DateTime Data { get; set; }
    }
}
