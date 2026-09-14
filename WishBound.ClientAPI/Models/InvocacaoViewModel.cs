namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// ViewModel da página de Invocação: banners disponíveis, probabilidades,
    /// estado das garantias e, depois de invocar, o resultado (1 ou 10).
    /// </summary>
    public class InvocacaoViewModel
    {
        public List<Raridade> Raridades { get; set; } = new List<Raridade>();

        public List<Banner> Banners { get; set; } = new List<Banner>();

        /// <summary>Banner escolhido (0 = o permanente, escolhido pela API).</summary>
        public int BannerId { get; set; }

        public Banner? BannerAtual => Banners.FirstOrDefault(b => b.Id == BannerId) ?? Banners.FirstOrDefault();

        /// <summary>Estado das garantias no banner escolhido (atalho para Estados[BannerId]).</summary>
        public EstadoPity? Estado => Estados.TryGetValue(BannerId, out var e) ? e : null;

        /// <summary>
        /// Estado das garantias em CADA banner a decorrer: a página mostra os
        /// painéis de todos e troca entre eles sem recarregar.
        /// </summary>
        public Dictionary<int, EstadoPity> Estados { get; set; } = new Dictionary<int, EstadoPity>();

        public EstadoPity? EstadoDe(int bannerId) => Estados.TryGetValue(bannerId, out var e) ? e : null;

        /// <summary>
        /// Detalhe (pool completa) de CADA banner, para a janela de
        /// probabilidades: mostra todas as personagens que podem sair e a
        /// probabilidade de cada uma.
        /// </summary>
        public Dictionary<int, BannerDetalhe> Detalhes { get; set; } = new Dictionary<int, BannerDetalhe>();

        public BannerDetalhe? DetalheDe(int bannerId) => Detalhes.TryGetValue(bannerId, out var d) ? d : null;

        public ResultadoInvocacao? Resultado { get; set; }

        /// <summary>
        /// Probabilidades por personagem num banner, agrupadas por raridade
        /// (da mais rara para a mais comum), como na tabela de um gacha real.
        /// Replica a regra da API: a raridade sai com a probabilidade da
        /// tabela; dentro dela, sem rate-up é ao acaso entre todas; com
        /// rate-up (só nos banners de evento) a quota (ex.: 80%) reparte-se
        /// pelas destacadas e o resto pelas restantes.
        /// </summary>
        public List<GrupoProbabilidade> ProbabilidadesDe(int bannerId)
        {
            var grupos = new List<GrupoProbabilidade>();
            var detalhe = DetalheDe(bannerId);
            if (detalhe == null) return grupos;

            foreach (var grupo in detalhe.PorRaridade)
            {
                var primeira = grupo.First();
                var raridade = Raridades.FirstOrDefault(r =>
                    string.Equals(r.Nome, primeira.RaridadeNome, StringComparison.OrdinalIgnoreCase));
                decimal probRaridade = raridade?.Probabilidade ?? 0m;

                var todas = grupo.ToList();
                // O rate-up só conta nos banners temporários
                var destaque = detalhe.Permanente ? new List<PersonagemDestaque>() : todas.Where(p => p.RateUp).ToList();
                var restantes = todas.Where(p => !destaque.Contains(p)).ToList();

                decimal quota = destaque.Count > 0 && restantes.Count > 0
                    ? (destaque.Select(p => p.Quota).FirstOrDefault(q => q.HasValue) ?? 0.5m)
                    : 0m;

                var linhas = new List<LinhaProbabilidade>();
                foreach (var p in todas)
                {
                    decimal prob;
                    if (destaque.Count == 0 || restantes.Count == 0)
                    {
                        prob = todas.Count > 0 ? probRaridade / todas.Count : 0m;
                    }
                    else if (destaque.Contains(p))
                    {
                        prob = probRaridade * quota / destaque.Count;
                    }
                    else
                    {
                        prob = probRaridade * (1 - quota) / restantes.Count;
                    }

                    linhas.Add(new LinhaProbabilidade { Personagem = p, Probabilidade = prob });
                }

                grupos.Add(new GrupoProbabilidade
                {
                    RaridadeNome = primeira.RaridadeNome,
                    Cor = primeira.CorOuOmissao,
                    RaridadeOrdem = primeira.RaridadeOrdem,
                    Probabilidade = probRaridade,
                    Quota = quota,
                    Linhas = linhas.OrderByDescending(l => l.Probabilidade).ThenBy(l => l.Personagem.Nome).ToList()
                });
            }

            return grupos;
        }
    }

    /// <summary>Uma raridade na janela de probabilidades, com as suas personagens.</summary>
    public class GrupoProbabilidade
    {
        public string RaridadeNome { get; set; } = string.Empty;
        public string Cor { get; set; } = "#9aa5b1";
        public int RaridadeOrdem { get; set; }
        public decimal Probabilidade { get; set; }

        /// <summary>Quota do rate-up nesta raridade (0 quando não há rate-up).</summary>
        public decimal Quota { get; set; }

        public bool TemRateUp => Quota > 0;

        public List<LinhaProbabilidade> Linhas { get; set; } = new List<LinhaProbabilidade>();

        public string ProbabilidadeTexto => FormatarPercentagem(Probabilidade);

        public string QuotaTexto => (Quota * 100m).ToString("0") + "%";

        /// <summary>"55%" / "2.5%" / "0.125%" — sem zeros a mais, sem arredondar a zero.</summary>
        public static string FormatarPercentagem(decimal fracao)
        {
            decimal pct = fracao * 100m;
            if (pct == 0) return "0%";
            if (pct >= 1) return pct.ToString("0.##") + "%";
            if (pct >= 0.01m) return pct.ToString("0.###") + "%";
            return pct.ToString("0.#####") + "%";
        }
    }

    /// <summary>Uma personagem e a probabilidade de sair numa invocação.</summary>
    public class LinhaProbabilidade
    {
        public PersonagemDestaque Personagem { get; set; } = new PersonagemDestaque();
        public decimal Probabilidade { get; set; }
        public string ProbabilidadeTexto => GrupoProbabilidade.FormatarPercentagem(Probabilidade);
    }
}
