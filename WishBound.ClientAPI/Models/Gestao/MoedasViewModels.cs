namespace WishBound.ClientAPI.Models.Gestao
{
    // ============================================================
    //  Modelos da gestão — MOEDAS (api/admin/moedas).
    //  Espelhos dos DTOs da WebAPI.
    // ============================================================

    public class TipoMoedaAdmin
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal EmCirculacao { get; set; }
        public int ContasComSaldo { get; set; }
        public decimal Ganho30Dias { get; set; }
        public decimal Gasto30Dias { get; set; }
        public int Transacoes { get; set; }
        public bool Protegida { get; set; }
        public bool PodeApagar { get; set; }
    }

    public class OrigemMoedaAdmin
    {
        public string Moeda { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int Movimentos { get; set; }
    }

    public class MoedasViewModel
    {
        public List<TipoMoedaAdmin> Tipos { get; set; } = new List<TipoMoedaAdmin>();
        public List<OrigemMoedaAdmin> OrigensGanho { get; set; } = new List<OrigemMoedaAdmin>();
        public List<OrigemMoedaAdmin> OrigensGasto { get; set; } = new List<OrigemMoedaAdmin>();
        public int ContasAtivas { get; set; }
    }
}
