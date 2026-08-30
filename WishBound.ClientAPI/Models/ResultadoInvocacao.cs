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

        /// <summary>Moedas gastas nesta invocação.</summary>
        public decimal CustoTotal { get; set; }

        /// <summary>Melhor raridade que saiu (para destacar a invocação de 10).</summary>
        public int MelhorOrdem => Personagens.Count == 0 ? 0 : Personagens.Max(p => p.RaridadeOrdem);
    }
}
