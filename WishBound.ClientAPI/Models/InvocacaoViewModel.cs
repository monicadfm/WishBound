namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// ViewModel da página de Invocação: banners disponíveis, probabilidades,
    /// estado das garantias e, depois de invocar, o resultado (1 ou 10).
    /// </summary>
    public class InvocacaoViewModel
    {
        public List<Raridade> Raridades { get; set; } = new List<Raridade>();

        public List<Banner> Banners { get; set; } = new List<Banner>();

        /// <summary>Banner escolhido (0 = o permanente, escolhido pela API).</summary>
        public int BannerId { get; set; }

        public Banner? BannerAtual => Banners.FirstOrDefault(b => b.Id == BannerId) ?? Banners.FirstOrDefault();

        public EstadoPity? Estado { get; set; }

        public ResultadoInvocacao? Resultado { get; set; }
    }
}
