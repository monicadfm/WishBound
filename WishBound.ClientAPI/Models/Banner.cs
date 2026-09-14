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

        // ----- Eventos e banners (Migracao08) -----

        public DateTime DataInicio { get; set; }

        /// <summary>Segundos até acabar (0 no permanente) — a contagem decrescente parte daqui.</summary>
        public long SegundosRestantes { get; set; }

        /// <summary>Personagens em destaque (rate-up).</summary>
        public List<PersonagemDestaque> Destaques { get; set; } = new List<PersonagemDestaque>();

        public int TotalExclusivas { get; set; }

        /// <summary>Montra do banner: até 3 personagens, a mais rara primeiro (centro do painel).</summary>
        public List<PersonagemDestaque> Vitrine { get; set; } = new List<PersonagemDestaque>();

        /// <summary>Personagem do centro do painel (a mais rara da montra).</summary>
        public PersonagemDestaque? Centro => Vitrine.FirstOrDefault();

        /// <summary>As duas dos lados do painel.</summary>
        public List<PersonagemDestaque> Lados => Vitrine.Skip(1).Take(2).ToList();

        /// <summary>Tem recompensas diárias (aparece também na Carteira).</summary>
        public bool TemRecompensas { get; set; }

        /// <summary>Evento já terminado — só um administrador o recebe (continua a poder invocar as exclusivas).</summary>
        public bool Terminado { get; set; }

        /// <summary>Data/hora do fim em UTC, no formato ISO, para o JavaScript da contagem decrescente.</summary>
        public string FimIso => DateTime.SpecifyKind(DataFim, DateTimeKind.Utc).ToString("o");

        /// <summary>"3 dias e 4 h" / "5 h e 12 min" / "terminado" — texto inicial da contagem (antes do JavaScript).</summary>
        public string TempoRestanteTexto
        {
            get
            {
                if (Permanente) return "permanente";
                if (SegundosRestantes <= 0) return "terminado";
                var t = TimeSpan.FromSeconds(SegundosRestantes);
                if (t.TotalDays >= 1) return (int)t.TotalDays + (t.TotalDays >= 2 ? " dias" : " dia") + " e " + t.Hours + " h";
                if (t.TotalHours >= 1) return t.Hours + " h e " + t.Minutes + " min";
                return t.Minutes + " min";
            }
        }
    }

    /// <summary>Personagem de um banner com o seu papel nele (espelho de PersonagemDestaque).</summary>
    public class PersonagemDestaque
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public int RaridadeOrdem { get; set; }
        public bool RateUp { get; set; }
        public decimal? Quota { get; set; }
        public bool Exclusiva { get; set; }
        public int Copias { get; set; }

        public string Imagem => string.IsNullOrEmpty(ImagemUrl) ? "/img/personagens/desconhecido.svg" : ImagemUrl;

        public string CorOuOmissao => string.IsNullOrEmpty(Cor) ? "#9aa5b1" : Cor;

        /// <summary>"80%" da quota do rate-up.</summary>
        public string QuotaTexto => Quota.HasValue ? (Quota.Value * 100).ToString("0") + "%" : "";
    }

    /// <summary>Detalhe de um banner para a página de Eventos (espelho de BannerDetalheResposta).</summary>
    public class BannerDetalhe : Banner
    {
        public List<PersonagemDestaque> Personagens { get; set; } = new List<PersonagemDestaque>();
        public List<string> RegrasRateUp { get; set; } = new List<string>();

        public int ContadorLendario { get; set; }
        public int ContadorEpico { get; set; }
        public bool GarantiaRateUp { get; set; }
        public int InvocacoesFeitas { get; set; }
        public int PersonagensObtidas { get; set; }
        public int DestaquesObtidos { get; set; }

        public int DiasRecompensa { get; set; }
        public int DiasResgatados { get; set; }
        public decimal TotalRecompensas { get; set; }
        public string? MoedaRecompensa { get; set; }

        /// <summary>Pool agrupada por raridade, da mais rara para a mais comum.</summary>
        public IEnumerable<IGrouping<int, PersonagemDestaque>> PorRaridade =>
            Personagens.GroupBy(p => p.RaridadeOrdem).OrderByDescending(g => g.Key);

        /// <summary>Míticas em destaque (as do 50/50).</summary>
        public List<PersonagemDestaque> MiticasDestaque => Destaques.Where(d => d.RaridadeOrdem >= 5).ToList();
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

        /// <summary>Bilhetes de invocação (cada um paga uma invocação inteira).</summary>
        public decimal SaldoBilhetes { get; set; }

        /// <summary>Ainda não recebeu a recompensa diária de hoje.</summary>
        public bool RecompensaDiariaDisponivel { get; set; }

        // ----- Eventos e banners -----

        public bool Permanente { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public long SegundosRestantes { get; set; }

        /// <summary>A próxima Mítica neste banner é garantida a do banner (perdeu o último 50/50).</summary>
        public bool GarantiaRateUp { get; set; }

        public List<PersonagemDestaque> Destaques { get; set; } = new List<PersonagemDestaque>();

        /// <summary>Míticas em destaque (as do 50/50).</summary>
        public List<PersonagemDestaque> MiticasDestaque => Destaques.Where(d => d.RaridadeOrdem >= 5).ToList();

        public string FimIso => DateTime.SpecifyKind(DataFim, DateTimeKind.Utc).ToString("o");

        /// <summary>Bilhetes que uma invocação de N usaria (gastos primeiro).</summary>
        public int BilhetesPara(int quantidade) => (int)Math.Min(quantidade, Math.Floor(SaldoBilhetes));

        /// <summary>Moedas que sobram para pagar depois dos bilhetes.</summary>
        public decimal MoedasPara(int quantidade) => CustoInvocacao * (quantidade - BilhetesPara(quantidade));

        /// <summary>Texto do preço no botão: "grátis", "3 bilhetes + 70 Moedas" ou "10 Moedas".</summary>
        public string PrecoTexto(int quantidade)
        {
            int bilhetes = BilhetesPara(quantidade);
            decimal moedas = MoedasPara(quantidade);

            if (bilhetes == quantidade)
            {
                return quantidade == 1 ? "1 bilhete" : quantidade + " bilhetes";
            }

            if (bilhetes > 0)
            {
                return bilhetes + (bilhetes == 1 ? " bilhete + " : " bilhetes + ") + moedas.ToString("0") + " Moedas";
            }

            return moedas.ToString("0") + " Moedas";
        }

        public bool PodeInvocar => SaldoMoedas >= MoedasPara(1);

        public bool PodeInvocarDez => SaldoMoedas >= MoedasPara(10);

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
