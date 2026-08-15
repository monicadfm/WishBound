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
}
