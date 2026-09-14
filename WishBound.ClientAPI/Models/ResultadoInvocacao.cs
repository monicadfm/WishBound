namespace WishBound.ClientAPI.Models
{
    /// <summary>Uma personagem saída numa invocação (espelho de PersonagemObtida).</summary>
    public class PersonagemObtida
    {
        public Personagem? Personagem { get; set; }
        public bool Novo { get; set; }
        public int Quantidade { get; set; }
        public bool PityAtivado { get; set; }
        public int RaridadeOrdem { get; set; }

        // Amizade: pontos dados por esta cópia (só repetidas), nível e subida
        public int PontosAmizadeGanhos { get; set; }
        public string? NivelAmizadeNome { get; set; }
        public bool SubiuDeNivel { get; set; }

        // Eventos e banners: rate-up, exclusiva e o 50/50 da Mítica
        public bool RateUp { get; set; }
        public bool Exclusiva { get; set; }
        public bool PerdeuCinquenta { get; set; }
        public bool GarantiaRateUpUsada { get; set; }

        public string Cor => Personagem?.Raridade?.Cor ?? "#9aa5b1";

        public string Imagem => string.IsNullOrEmpty(Personagem?.ImagemUrl)
            ? "/img/personagens/desconhecido.svg"
            : Personagem!.ImagemUrl!;
    }

    /// <summary>
    /// Resultado de uma invocação (1 ou 10) devolvido pela WebAPI:
    /// personagens obtidas, espaço da coleção e contadores de garantia.
    /// </summary>
    public class ResultadoInvocacao
    {
        public List<PersonagemObtida> Personagens { get; set; } = new List<PersonagemObtida>();

        /// <summary>Subidas de nível de amizade nesta invocação (uma frase por personagem).</summary>
        public List<string> MensagensAmizade { get; set; } = new List<string>();

        public int BannerId { get; set; }
        public string BannerNome { get; set; } = string.Empty;

        public int Ocupado { get; set; }
        public int Capacidade { get; set; }

        public int ContadorLendario { get; set; }
        public int ContadorEpico { get; set; }
        public int FaltamParaLendario { get; set; }
        public int FaltamParaEpico { get; set; }

        /// <summary>Saldo em Moedas depois de pagar.</summary>
        public decimal SaldoMoedas { get; set; }

        /// <summary>Moedas gastas nesta invocação (0 se paga só com bilhetes).</summary>
        public decimal CustoTotal { get; set; }

        /// <summary>Bilhetes de invocação gastos.</summary>
        public int BilhetesUsados { get; set; }

        /// <summary>Bilhetes que sobram.</summary>
        public decimal SaldoBilhetes { get; set; }

        /// <summary>A próxima Mítica neste banner é garantida a do banner.</summary>
        public bool GarantiaRateUp { get; set; }

        /// <summary>"3 bilhetes + 70 Moedas gastas" / "100 Moedas gastas" / "1 bilhete gasto".</summary>
        public string GastoTexto
        {
            get
            {
                string bilhetes = BilhetesUsados == 0 ? "" : BilhetesUsados + (BilhetesUsados == 1 ? " bilhete" : " bilhetes");
                string moedas = CustoTotal <= 0 ? "" : CustoTotal.ToString("0") + " Moedas";

                if (bilhetes.Length > 0 && moedas.Length > 0)
                {
                    return bilhetes + " + " + moedas + " gastos";
                }

                if (bilhetes.Length > 0)
                {
                    return bilhetes + (BilhetesUsados == 1 ? " gasto" : " gastos");
                }

                return moedas + " gastas";
            }
        }

        /// <summary>Melhor raridade que saiu (para destacar a invocação de 10).</summary>
        public int MelhorOrdem => Personagens.Count == 0 ? 0 : Personagens.Max(p => p.RaridadeOrdem);
    }
}
