namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Pedido de invocação enviado pelo site: identifica o utilizador
    /// autenticado em nome de quem a invocação é registada.
    /// (No futuro levará também o BannerId, quando existirem banners de evento.)
    /// </summary>
    public class InvocacaoPedido
    {
        public int UtilizadorId { get; set; }
    }

    /// <summary>
    /// Resultado de uma invocação: a personagem obtida e o efeito que teve na
    /// coleção do utilizador (nova ou repetida, quantas cópias tem agora e
    /// quanto espaço lhe resta).
    /// </summary>
    public class InvocacaoResultado
    {
        public Personagem? Personagem { get; set; }

        /// <summary>true quando é a primeira vez que obtém esta personagem.</summary>
        public bool Novo { get; set; }

        /// <summary>Cópias que passa a ter desta personagem.</summary>
        public int Quantidade { get; set; }

        /// <summary>Espaço ocupado depois desta invocação.</summary>
        public int Ocupado { get; set; }

        public int Capacidade { get; set; }
    }
}
