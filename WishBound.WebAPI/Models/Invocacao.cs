namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Registo de uma invocação (obtenção aleatória de uma personagem).
    /// Serve de histórico e permite demonstrar operações SELECT/INSERT.
    /// </summary>
    public class Invocacao
    {
        public int Id { get; set; }

        public int PersonagemId { get; set; }

        public Personagem? Personagem { get; set; }

        public DateTime Data { get; set; } = DateTime.Now;
    }
}
