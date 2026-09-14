namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// ViewModel da página de Eventos: todos os banners a decorrer e o
    /// detalhe do escolhido (pool, rate-up, participação do utilizador).
    /// </summary>
    public class EventosViewModel
    {
        public List<Banner> Banners { get; set; } = new List<Banner>();

        public BannerDetalhe? Detalhe { get; set; }

        /// <summary>Só os banners temporários (os eventos).</summary>
        public List<Banner> Eventos => Banners.Where(b => !b.Permanente).ToList();

        public Banner? Permanente => Banners.FirstOrDefault(b => b.Permanente);
    }
}
