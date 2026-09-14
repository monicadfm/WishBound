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

        // ----- Eventos e banners (rate-up) -----

        /// <summary>É uma das personagens em destaque (rate-up) do banner.</summary>
        public bool RateUp { get; set; }

        /// <summary>Exclusiva de evento: não existe no banner permanente.</summary>
        public bool Exclusiva { get; set; }

        /// <summary>Mítica que saiu do lado "permanente" do 50/50 — a próxima Mítica neste banner é garantida a do banner.</summary>
        public bool PerdeuCinquenta { get; set; }

        /// <summary>Mítica do banner que saiu por a garantia do 50/50 estar ativa.</summary>
        public bool GarantiaRateUpUsada { get; set; }
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

        /// <summary>Depois desta invocação, a próxima Mítica neste banner é garantida a do banner (perdeu o 50/50).</summary>
        public bool GarantiaRateUp { get; set; }
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

        // ----- Eventos e banners -----

        /// <summary>Banner permanente (sem fim) ou de evento.</summary>
        public bool Permanente { get; set; }

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        /// <summary>Segundos até o banner acabar (0 no permanente / já terminado) — para a contagem decrescente.</summary>
        public long SegundosRestantes { get; set; }

        /// <summary>A próxima Mítica neste banner é garantida a do banner (o utilizador perdeu o último 50/50).</summary>
        public bool GarantiaRateUp { get; set; }

        /// <summary>Personagens em destaque (rate-up) do banner.</summary>
        public List<PersonagemDestaque> Destaques { get; set; } = new List<PersonagemDestaque>();
    }

    /// <summary>Personagem de um banner, com o seu papel nele (destaque, exclusiva) — usada nas listas de banners.</summary>
    public class PersonagemDestaque
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public int RaridadeOrdem { get; set; }

        /// <summary>Em destaque (rate-up) neste banner.</summary>
        public bool RateUp { get; set; }

        /// <summary>Quota do rate-up dentro da raridade (0.80 = 80%); só nas rate-up.</summary>
        public decimal? Quota { get; set; }

        /// <summary>Não existe no banner permanente — só se invoca enquanto o evento durar.</summary>
        public bool Exclusiva { get; set; }

        /// <summary>Cópias que o utilizador tem (0 = ainda não a obteve). Só quando o pedido traz utilizadorId.</summary>
        public int Copias { get; set; }
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

        // ----- Eventos e banners (Migracao08) -----

        public DateTime DataInicio { get; set; }

        /// <summary>Segundos até acabar (0 no permanente) — a página faz a contagem decrescente a partir daqui.</summary>
        public long SegundosRestantes { get; set; }

        /// <summary>Personagens em destaque (rate-up) — vazio no permanente.</summary>
        public List<PersonagemDestaque> Destaques { get; set; } = new List<PersonagemDestaque>();

        /// <summary>Quantas personagens do banner são exclusivas de evento.</summary>
        public int TotalExclusivas { get; set; }

        /// <summary>
        /// Montra do banner (até 3 personagens para o painel da página de
        /// invocação): as em destaque (rate-up) se as houver, senão as mais
        /// raras da pool — a mais rara primeiro (fica ao centro, em grande).
        /// </summary>
        public List<PersonagemDestaque> Vitrine { get; set; } = new List<PersonagemDestaque>();

        /// <summary>Tem recompensas diárias (linhas em RecompensasEvento) — aparece também na Carteira.</summary>
        public bool TemRecompensas { get; set; }

        /// <summary>
        /// Já acabou (ou ainda não começou). Só os ADMINISTRADORES recebem
        /// banners nesta situação: continuam a ver e a invocar as personagens
        /// exclusivas depois de o evento terminar.
        /// </summary>
        public bool Terminado { get; set; }
    }

    /// <summary>
    /// Detalhe de um banner para a página de Eventos: a pool completa por
    /// raridade, quem está em destaque, o que é exclusivo e — com
    /// utilizadorId — a participação do utilizador (garantias, invocações
    /// feitas, personagens já obtidas, recompensas do evento).
    /// </summary>
    public class BannerDetalheResposta : BannerResposta
    {
        /// <summary>Pool completa do banner, por raridade (da mais rara para a mais comum).</summary>
        public List<PersonagemDestaque> Personagens { get; set; } = new List<PersonagemDestaque>();

        /// <summary>Regras do rate-up em texto ("Lendário: 80% …"), uma por raridade com destaque.</summary>
        public List<string> RegrasRateUp { get; set; } = new List<string>();

        // ----- Participação do utilizador (só com utilizadorId) -----

        public int ContadorLendario { get; set; }
        public int ContadorEpico { get; set; }
        public bool GarantiaRateUp { get; set; }

        /// <summary>Invocações que o utilizador já fez neste banner.</summary>
        public int InvocacoesFeitas { get; set; }

        /// <summary>Quantas das personagens do banner o utilizador já tem.</summary>
        public int PersonagensObtidas { get; set; }

        /// <summary>Quantas das personagens em destaque o utilizador já tem.</summary>
        public int DestaquesObtidos { get; set; }

        // ----- Recompensas diárias do evento (se existirem) -----

        public int DiasRecompensa { get; set; }
        public int DiasResgatados { get; set; }
        public decimal TotalRecompensas { get; set; }
        public string? MoedaRecompensa { get; set; }
    }
}
