namespace WishBound.ClientAPI.Models.Economia
{
    // ============================================================
    //  Modelos da página "Carteira" (espelhos dos DTOs de api/economia).
    // ============================================================

    /// <summary>Saldo numa moeda.</summary>
    public class Carteira
    {
        public int TipoMoedaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Saldo { get; set; }
    }

    /// <summary>Um dia do calendário de recompensas diárias (28 dias).</summary>
    public class DiaRecompensa
    {
        public int Dia { get; set; }
        public int Semana { get; set; }
        public int TipoMoedaId { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public bool FimDeSemana { get; set; }
        public bool Recebido { get; set; }
        public bool Hoje { get; set; }

        /// <summary>"20 Moedas" / "1 Bilhete" / "3 Bilhetes".</summary>
        public string Texto => Formatar(Quantidade, MoedaNome);

        public static string Formatar(decimal quantidade, string moeda)
        {
            string q = quantidade.ToString("0");

            if (quantidade == 1 && moeda.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                return q + " " + moeda.Substring(0, moeda.Length - 1);
            }

            return q + " " + moeda;
        }
    }

    /// <summary>Estado da recompensa diária.</summary>
    public class RecompensaDiaria
    {
        public int DiasRecebidos { get; set; }
        public bool RecebidaHoje { get; set; }
        public int ProximoDia { get; set; }
        public List<DiaRecompensa> Calendario { get; set; } = new List<DiaRecompensa>();

        /// <summary>O dia que está em destaque (o de hoje).</summary>
        public DiaRecompensa? DiaDeHoje => Calendario.FirstOrDefault(d => d.Hoje);

        /// <summary>Calendário agrupado por semana, para a grelha.</summary>
        public IEnumerable<IGrouping<int, DiaRecompensa>> PorSemana => Calendario.GroupBy(d => d.Semana);
    }

    /// <summary>Um dia de um evento.</summary>
    public class DiaEvento
    {
        public int Dia { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public int? TipoMoedaId { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public bool Recebido { get; set; }
        public bool Hoje { get; set; }

        public string Texto => DiaRecompensa.Formatar(Quantidade, MoedaNome);
    }

    /// <summary>Evento a decorrer com a participação do utilizador.</summary>
    public class Evento
    {
        public int BannerId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int DiasRestantes { get; set; }
        public int Progresso { get; set; }
        public bool RecebidoHoje { get; set; }
        public bool Concluido { get; set; }
        public decimal TotalRecompensas { get; set; }
        public List<DiaEvento> Dias { get; set; } = new List<DiaEvento>();

        public bool PodeResgatar => !Concluido && !RecebidoHoje;

        public DiaEvento? DiaDeHoje => Dias.FirstOrDefault(d => d.Hoje);
    }

    /// <summary>Tudo o que a página Carteira mostra.</summary>
    public class EconomiaViewModel
    {
        public List<Carteira> Carteiras { get; set; } = new List<Carteira>();
        public decimal SaldoMoedas { get; set; }
        public decimal SaldoBilhetes { get; set; }
        public decimal CustoInvocacao { get; set; } = 10;
        public RecompensaDiaria RecompensaDiaria { get; set; } = new RecompensaDiaria();
        public List<Evento> Eventos { get; set; } = new List<Evento>();

        /// <summary>Invocações que o saldo atual permite (Moedas + Bilhetes).</summary>
        public int InvocacoesPossiveis => CustoInvocacao <= 0
            ? 0
            : (int)Math.Floor(SaldoMoedas / CustoInvocacao) + (int)Math.Floor(SaldoBilhetes);
    }

    /// <summary>Um movimento do histórico de transações.</summary>
    public class Transacao
    {
        public int Id { get; set; }
        public int TipoMoedaId { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Montante { get; set; }
        public string TipoTransacao { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
        public DateTime Data { get; set; }

        public bool Ganho => TipoTransacao == "Ganho";
    }

    /// <summary>Resposta de "receber recompensa".</summary>
    public class RecompensaRecebida
    {
        public string Mensagem { get; set; } = string.Empty;
        public int TipoMoedaId { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal NovoSaldo { get; set; }
        public int Dia { get; set; }
    }
}
