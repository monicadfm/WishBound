namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// Resultado de uma invocação devolvido pela WebAPI: a personagem obtida
    /// e o efeito na coleção (nova ou repetida, quantas cópias tem agora e
    /// quanto espaço está ocupado).
    /// </summary>
    public class ResultadoInvocacao
    {
        public Personagem? Personagem { get; set; }
        public bool Novo { get; set; }
        public int Quantidade { get; set; }
        public int Ocupado { get; set; }
        public int Capacidade { get; set; }
    }
}
