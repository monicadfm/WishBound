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

        /// <summary>Estado das garantias no banner escolhido (atalho para Estados[BannerId]).</summary>
        public EstadoPity? Estado => Estados.TryGetValue(BannerId, out var e) ? e : null;

        /// <summary>
        /// Estado das garantias em CADA banner a decorrer: a página mostra os
        /// painéis de todos e troca entre eles sem recarregar.
        /// </summary>
        public Dictionary<int, EstadoPity> Estados { get; set; } = new Dictionary<int, EstadoPity>();

        public EstadoPity? EstadoDe(int bannerId) => Estados.TryGetValue(bannerId, out var e) ? e : null;

        public ResultadoInvocacao? Resultado { get; set; }
    }
}
