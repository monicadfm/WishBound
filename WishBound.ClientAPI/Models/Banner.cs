namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// Banner de invocação devolvido pela WebAPI (espelho de BannerResposta).
    /// </summary>
    public class Banner
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string TipoBanner { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public DateTime DataFim { get; set; }
        public int TotalPersonagens { get; set; }
        public bool Permanente { get; set; }
    }

    /// <summary>
    /// Estado das garantias (pity) do utilizador num banner
    /// (espelho de EstadoPityResposta).
    /// </summary>
    public class EstadoPity
    {
        public int BannerId { get; set; }
        public string BannerNome { get; set; } = string.Empty;

        public int ContadorLendario { get; set; }
        public int ContadorEpico { get; set; }
        public int FaltamParaLendario { get; set; }
        public int FaltamParaEpico { get; set; }

        /// <summary>Saldo do utilizador em Moedas.</summary>
        public decimal SaldoMoedas { get; set; }

        /// <summary>Custo de cada invocação (10 Moedas).</summary>
        public decimal CustoInvocacao { get; set; }

        /// <summary>Custo de uma invocação x10.</summary>
        public decimal CustoDez => CustoInvocacao * 10;

        public bool PodeInvocar => SaldoMoedas >= CustoInvocacao;

        public bool PodeInvocarDez => SaldoMoedas >= CustoDez;

        public int LimiteLendario { get; set; } = 90;
        public int LimiteEpico { get; set; } = 10;
        public int InicioSoftPity { get; set; } = 81;

        /// <summary>Percentagem do contador dos 90, para a barra de progresso.</summary>
        public int PercentagemLendario => LimiteLendario <= 0
            ? 0
            : (int)Math.Min(100, Math.Round(ContadorLendario * 100.0 / LimiteLendario));

        /// <summary>Já está na fase em que a probabilidade sobe a cada invocação?</summary>
        public bool EmSoftPity => ContadorLendario >= InicioSoftPity - 1;
    }
}
