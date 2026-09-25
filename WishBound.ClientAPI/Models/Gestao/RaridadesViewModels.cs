namespace WishBound.ClientAPI.Models.Gestao
{
    // ============================================================
    //  Modelos da gestão — RARIDADES (api/admin/raridades).
    //  Espelhos dos DTOs da WebAPI.
    // ============================================================

    public class RaridadeAdmin
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public decimal Probabilidade { get; set; }
        public decimal PercentagemEfetiva { get; set; }
        public int Ordem { get; set; }
        public int NivelAmizadeMaximo { get; set; }
        public int Personagens { get; set; }
        public int Invocacoes { get; set; }
        public bool PodeApagar { get; set; }

        /// <summary>Probabilidade em % para os formulários (0.0250 → "2.5").</summary>
        public string PercentagemFormulario =>
            (Probabilidade * 100m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    public class RaridadesViewModel
    {
        public List<RaridadeAdmin> Itens { get; set; } = new List<RaridadeAdmin>();
        public decimal SomaProbabilidades { get; set; }

        public bool SomaCerta => Math.Abs(SomaProbabilidades - 1m) <= 0.0001m;
    }
}
