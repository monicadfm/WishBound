namespace WishBound.ClientAPI.Models.Gestao
{
    // ============================================================
    //  Modelos da gestão — ESTATÍSTICAS (api/admin/estatisticas).
    //  Espelhos dos DTOs da WebAPI.
    // ============================================================

    public class DistribuicaoRaridadeAdmin
    {
        public string Raridade { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public int Ordem { get; set; }
        public int Obtidas { get; set; }
        public decimal Percentagem { get; set; }
        public decimal PercentagemEsperada { get; set; }
    }

    public class PersonagemPopularAdmin
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public int Contas { get; set; }
        public int Copias { get; set; }
    }

    public class ContagemAdmin
    {
        public string Nome { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public int Total { get; set; }
    }

    public class EconomiaMoedaAdmin
    {
        public string Moeda { get; set; } = string.Empty;
        public decimal EmCirculacao { get; set; }
        public decimal Ganho { get; set; }
        public decimal Gasto { get; set; }
    }

    public class EventoParticipacaoAdmin
    {
        public int BannerId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int Participantes { get; set; }
        public int ContasQueInvocaram { get; set; }
        public int Invocacoes { get; set; }
        public int ContasComRecompensas { get; set; }
        public int ContasConcluiram { get; set; }
    }

    public class EstatisticasViewModel
    {
        public int Dias { get; set; } = 30;
        public DateTime GeradoEm { get; set; }
        public int UtilizadoresAtivos { get; set; }
        public int TotalInvocacoes { get; set; }
        public int TotalPersonagensObtidas { get; set; }
        public int ContasTotal { get; set; }
        public int NovasContas { get; set; }
        public int InvocacoesPeriodo { get; set; }
        public int InvocacoesComPity { get; set; }
        public int ContasComLogin { get; set; }
        public List<SerieDia> InvocacoesPorDia { get; set; } = new List<SerieDia>();
        public List<SerieDia> ContasPorDia { get; set; } = new List<SerieDia>();
        public List<DistribuicaoRaridadeAdmin> DistribuicaoRaridades { get; set; } = new List<DistribuicaoRaridadeAdmin>();
        public List<PersonagemPopularAdmin> PersonagensPopulares { get; set; } = new List<PersonagemPopularAdmin>();
        public List<ContagemAdmin> InvocacoesPorBanner { get; set; } = new List<ContagemAdmin>();
        public List<EconomiaMoedaAdmin> Economia { get; set; } = new List<EconomiaMoedaAdmin>();
        public List<ContagemAdmin> NiveisAmizade { get; set; } = new List<ContagemAdmin>();
        public List<ContagemAdmin> AcoesPorCategoria { get; set; } = new List<ContagemAdmin>();
        public List<EventoParticipacaoAdmin> EventosParticipacao { get; set; } = new List<EventoParticipacaoAdmin>();
    }
}
