using System.ComponentModel.DataAnnotations;

namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs da economia virtual (o que api/economia devolve ao site).
    // ============================================================

    /// <summary>Saldo do utilizador numa moeda, já com o nome da moeda.</summary>
    public class CarteiraResposta
    {
        public int TipoMoedaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Saldo { get; set; }
    }

    /// <summary>Um dia do calendário de recompensas diárias (28 dias).</summary>
    public class DiaRecompensa
    {
        /// <summary>1..28</summary>
        public int Dia { get; set; }

        /// <summary>1..4</summary>
        public int Semana { get; set; }

        public int TipoMoedaId { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }

        /// <summary>Último dia da semana — recompensa maior.</summary>
        public bool FimDeSemana { get; set; }

        /// <summary>Já foi recebido neste ciclo.</summary>
        public bool Recebido { get; set; }

        /// <summary>É o dia de hoje (o próximo a receber, ou o já recebido hoje).</summary>
        public bool Hoje { get; set; }
    }

    /// <summary>Estado da recompensa diária do utilizador.</summary>
    public class RecompensaDiariaResposta
    {
        /// <summary>Dias já recebidos no ciclo atual (0..28).</summary>
        public int DiasRecebidos { get; set; }

        /// <summary>Já recebeu a recompensa de hoje?</summary>
        public bool RecebidaHoje { get; set; }

        /// <summary>O dia que recebe a seguir (1..28).</summary>
        public int ProximoDia { get; set; }

        public List<DiaRecompensa> Calendario { get; set; } = new List<DiaRecompensa>();
    }

    /// <summary>Um dia de um evento de recompensas.</summary>
    public class DiaEvento
    {
        public int Dia { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public int? TipoMoedaId { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public bool Recebido { get; set; }
        public bool Hoje { get; set; }
    }

    /// <summary>Evento a decorrer e a participação do utilizador nele.</summary>
    public class EventoResposta
    {
        public int BannerId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        /// <summary>Dias inteiros que faltam até o evento acabar (0 = acaba hoje).</summary>
        public int DiasRestantes { get; set; }

        /// <summary>Dias já resgatados pelo utilizador.</summary>
        public int Progresso { get; set; }

        public bool RecebidoHoje { get; set; }

        /// <summary>Já recebeu todos os dias do evento.</summary>
        public bool Concluido { get; set; }

        /// <summary>Total de moeda que o evento dá a quem receber todos os dias.</summary>
        public decimal TotalRecompensas { get; set; }

        public List<DiaEvento> Dias { get; set; } = new List<DiaEvento>();
    }

    /// <summary>Tudo o que a página "Carteira" mostra de uma vez.</summary>
    public class EconomiaResposta
    {
        public List<CarteiraResposta> Carteiras { get; set; } = new List<CarteiraResposta>();

        public decimal SaldoMoedas { get; set; }
        public decimal SaldoBilhetes { get; set; }

        /// <summary>Quanto custa uma invocação em Moedas (para "dá para N invocações").</summary>
        public decimal CustoInvocacao { get; set; }

        public RecompensaDiariaResposta RecompensaDiaria { get; set; } = new RecompensaDiariaResposta();

        public List<EventoResposta> Eventos { get; set; } = new List<EventoResposta>();
    }

    /// <summary>Um movimento do histórico de transações.</summary>
    public class TransacaoResposta
    {
        public int Id { get; set; }
        public int TipoMoedaId { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Montante { get; set; }

        /// <summary>"Ganho" ou "Gasto".</summary>
        public string TipoTransacao { get; set; } = string.Empty;

        public string Origem { get; set; } = string.Empty;
        public DateTime Data { get; set; }
    }

    /// <summary>Resultado de receber uma recompensa (diária ou de evento).</summary>
    public class RecompensaRecebidaResposta
    {
        public string Mensagem { get; set; } = string.Empty;
        public int TipoMoedaId { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal NovoSaldo { get; set; }

        /// <summary>Dia recebido (do calendário ou do evento).</summary>
        public int Dia { get; set; }
    }

    /// <summary>Receber a recompensa diária.</summary>
    public class LoginDiarioPedido
    {
        [Required]
        public int UtilizadorId { get; set; }
    }

    /// <summary>Receber a recompensa do dia num evento.</summary>
    public class ResgatarEventoPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        [Required]
        public int BannerId { get; set; }
    }
}
