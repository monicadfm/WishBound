using System.ComponentModel.DataAnnotations;

namespace WishBound.WebAPI.Models
{
    /// <summary>
    /// Pedido de invocação enviado pelo site: quem invoca, em que banner e
    /// quantas invocações de uma vez (1 ou 10).
    /// </summary>
    public class InvocacaoPedido
    {
        public int UtilizadorId { get; set; }

        /// <summary>Banner escolhido. 0 = usa o banner permanente ativo.</summary>
        public int BannerId { get; set; }

        [Range(1, 10, ErrorMessage = "Só é possível invocar 1 ou 10 de cada vez.")]
        public int Quantidade { get; set; } = 1;
    }

    /// <summary>Uma personagem saída numa invocação.</summary>
    public class PersonagemObtida
    {
        public Personagem? Personagem { get; set; }

        /// <summary>Primeira vez que o utilizador obtém esta personagem.</summary>
        public bool Novo { get; set; }

        /// <summary>Cópias que passa a ter desta personagem.</summary>
        public int Quantidade { get; set; }

        /// <summary>Saiu por garantia (pity) e não pela sorte normal.</summary>
        public bool PityAtivado { get; set; }

        public int RaridadeOrdem { get; set; }

        // ----- Sistema de amizade -----

        /// <summary>Pontos de amizade dados por esta cópia (só as repetidas dão; 0 numa personagem nova).</summary>
        public int PontosAmizadeGanhos { get; set; }

        /// <summary>Nome do nível de amizade com a personagem depois desta invocação.</summary>
        public string? NivelAmizadeNome { get; set; }

        /// <summary>A amizade com a personagem subiu de nível nesta invocação.</summary>
        public bool SubiuDeNivel { get; set; }
    }

    /// <summary>
    /// Resultado de uma invocação simples ou de uma de 10: as personagens
    /// obtidas, o espaço da coleção e como ficaram os contadores de pity.
    /// </summary>
    public class InvocacaoResultado
    {
        public List<PersonagemObtida> Personagens { get; set; } = new List<PersonagemObtida>();

        /// <summary>Subidas de nível de amizade e recompensas desbloqueadas nesta invocação.</summary>
        public List<string> MensagensAmizade { get; set; } = new List<string>();

        public int BannerId { get; set; }
        public string BannerNome { get; set; } = string.Empty;

        public int Ocupado { get; set; }
        public int Capacidade { get; set; }

        /// <summary>Invocações desde a última Lendária/Mítica.</summary>
        public int ContadorLendario { get; set; }

        /// <summary>Invocações desde a última Épica ou melhor.</summary>
        public int ContadorEpico { get; set; }

        /// <summary>Quantas invocações faltam para a garantia de Lendária/Mítica.</summary>
        public int FaltamParaLendario { get; set; }

        /// <summary>Quantas invocações faltam para a garantia de Épica.</summary>
        public int FaltamParaEpico { get; set; }

        /// <summary>Saldo em Moedas depois de pagar as invocações.</summary>
        public decimal SaldoMoedas { get; set; }

        /// <summary>Moedas gastas nesta invocação (0 se foi toda paga com bilhetes).</summary>
        public decimal CustoTotal { get; set; }

        /// <summary>Bilhetes de invocação gastos nesta invocação.</summary>
        public int BilhetesUsados { get; set; }

        /// <summary>Bilhetes que ainda tem depois de invocar.</summary>
        public decimal SaldoBilhetes { get; set; }
    }

    /// <summary>
    /// Estado do pity de um utilizador num banner (mostrado na página de
    /// invocação antes de invocar).
    /// </summary>
    public class EstadoPityResposta
    {
        public int BannerId { get; set; }
        public string BannerNome { get; set; } = string.Empty;

        public int ContadorLendario { get; set; }
        public int ContadorEpico { get; set; }
        public int FaltamParaLendario { get; set; }
        public int FaltamParaEpico { get; set; }

        /// <summary>90 — invocações até à garantia de Lendária/Mítica.</summary>
        public int LimiteLendario { get; set; }

        /// <summary>10 — invocações até à garantia de Épica.</summary>
        public int LimiteEpico { get; set; }

        /// <summary>Invocação a partir da qual a probabilidade começa a subir (81).</summary>
        public int InicioSoftPity { get; set; }

        /// <summary>Saldo do utilizador em Moedas.</summary>
        public decimal SaldoMoedas { get; set; }

        /// <summary>Quanto custa cada invocação (10 Moedas).</summary>
        public decimal CustoInvocacao { get; set; }

        /// <summary>Bilhetes de invocação do utilizador (cada um paga uma invocação).</summary>
        public decimal SaldoBilhetes { get; set; }

        /// <summary>Ainda não recebeu a recompensa diária de hoje (para a página avisar).</summary>
        public bool RecompensaDiariaDisponivel { get; set; }
    }

    /// <summary>Banner apresentado ao utilizador na página de invocação.</summary>
    public class BannerResposta
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string TipoBanner { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public DateTime DataFim { get; set; }

        /// <summary>Personagens ativas incluídas neste banner.</summary>
        public int TotalPersonagens { get; set; }

        /// <summary>Banner permanente (Standard) ou de evento, com fim marcado.</summary>
        public bool Permanente { get; set; }
    }
}
