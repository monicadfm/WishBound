namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs — CRUD DAS RARIDADES (api/admin/raridades).
    //  Os pedidos de escrita trazem sempre o AdminId e um Motivo
    //  opcional, que fica no registo de ações (LogsAdministrador).
    // ============================================================

    public class AdminRaridadeResposta
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Cor { get; set; }

        /// <summary>Peso guardado na BD (fração: 0.55 = 55%).</summary>
        public decimal Probabilidade { get; set; }

        /// <summary>Probabilidade efetiva (peso / soma dos pesos), em %.</summary>
        public decimal PercentagemEfetiva { get; set; }

        public int Ordem { get; set; }

        /// <summary>Nível de amizade máximo que as personagens desta raridade alcançam.</summary>
        public int NivelAmizadeMaximo { get; set; }

        public int Personagens { get; set; }
        public int Invocacoes { get; set; }

        /// <summary>Só se apaga uma raridade sem personagens nem histórico.</summary>
        public bool PodeApagar { get; set; }
    }

    public class AdminListaRaridades
    {
        public List<AdminRaridadeResposta> Itens { get; set; } = new List<AdminRaridadeResposta>();

        /// <summary>Soma dos pesos (o ideal é 1.0000 = 100%).</summary>
        public decimal SomaProbabilidades { get; set; }
    }

    public class AdminRaridadePedido
    {
        public int AdminId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Cor { get; set; }

        /// <summary>Fração 0.0001 .. 1 (o site converte a percentagem).</summary>
        public decimal Probabilidade { get; set; }

        /// <summary>Escalão 1..5 (1 = Comum ... 5 = Mítico) — decide pity e amizade.</summary>
        public int Ordem { get; set; }

        public string? Motivo { get; set; }
    }
}
