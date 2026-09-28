namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs — ESTATÍSTICAS (api/admin/estatisticas) + as linhas das três vistas
    //  da base de dados, lidas com Database.SqlQuery.
    //  Os pedidos de escrita trazem sempre o AdminId e um Motivo
    //  opcional, que fica no registo de ações (LogsAdministrador).
    // ============================================================

    public class AdminDistribuicaoRaridade
    {
        public string Raridade { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public int Ordem { get; set; }
        public int Obtidas { get; set; }
        public decimal Percentagem { get; set; }
        public decimal PercentagemEsperada { get; set; }
    }

    public class AdminPersonagemPopular
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }

        /// <summary>Contas que a têm (vw_PersonagensMaisPopulares).</summary>
        public int Contas { get; set; }

        public int Copias { get; set; }
    }

    public class AdminContagem
    {
        public string Nome { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public int Total { get; set; }
    }

    public class AdminEconomiaMoeda
    {
        public string Moeda { get; set; } = string.Empty;
        public decimal EmCirculacao { get; set; }
        public decimal Ganho { get; set; }
        public decimal Gasto { get; set; }
    }

    public class AdminEstatisticasResposta
    {
        public int Dias { get; set; }
        public DateTime GeradoEm { get; set; }

        // vw_EstatisticasGerais
        public int UtilizadoresAtivos { get; set; }
        public int TotalInvocacoes { get; set; }
        public int TotalPersonagensObtidas { get; set; }

        public int ContasTotal { get; set; }
        public int NovasContas { get; set; }
        public int InvocacoesPeriodo { get; set; }
        public int InvocacoesComPity { get; set; }
        public int ContasComLogin { get; set; }

        public List<AdminSerieDia> InvocacoesPorDia { get; set; } = new List<AdminSerieDia>();
        public List<AdminSerieDia> ContasPorDia { get; set; } = new List<AdminSerieDia>();

        // vw_DistribuicaoRaridades
        public List<AdminDistribuicaoRaridade> DistribuicaoRaridades { get; set; } = new List<AdminDistribuicaoRaridade>();

        // vw_PersonagensMaisPopulares
        public List<AdminPersonagemPopular> PersonagensPopulares { get; set; } = new List<AdminPersonagemPopular>();

        public List<AdminContagem> InvocacoesPorBanner { get; set; } = new List<AdminContagem>();
        public List<AdminEconomiaMoeda> Economia { get; set; } = new List<AdminEconomiaMoeda>();
        public List<AdminContagem> NiveisAmizade { get; set; } = new List<AdminContagem>();
        public List<AdminContagem> AcoesPorCategoria { get; set; } = new List<AdminContagem>();
    }

    public class VistaEstatisticasGerais
    {
        public int? TotalUtilizadores { get; set; }
        public int? TotalInvocacoes { get; set; }
        public int? TotalPersonagensObtidas { get; set; }
    }

    public class VistaDistribuicaoRaridades
    {
        public string Raridade { get; set; } = string.Empty;
        public int TotalObtidas { get; set; }
    }

    public class VistaPersonagensMaisPopulares
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int TotalObtencoes { get; set; }
    }
}
