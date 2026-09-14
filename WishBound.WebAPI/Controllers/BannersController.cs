using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Banners de invocação: o permanente e os de evento (temporários, com
    /// contagem decrescente, personagens EXCLUSIVAS e RATE-UP — Migracao08).
    ///
    ///   GET api/banners               - os que estão a decorrer, com as
    ///                                   personagens em destaque de cada um;
    ///   GET api/banners/{id}?utilizadorId=N
    ///                                 - detalhe para a página de Eventos:
    ///                                   pool completa, regras do rate-up e a
    ///                                   participação do utilizador (garantias,
    ///                                   invocações feitas, personagens já
    ///                                   obtidas, recompensas do evento).
    ///
    /// O site usa a lista para deixar escolher onde invocar — antes o banner
    /// estava fixo no código (Id 1).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BannersController : ControllerBase
    {
        /// <summary>Quota do rate-up quando a linha do banner não a indica.</summary>
        private const decimal QuotaRateUpPorOmissao = 0.5m;

        private const int OrdemMitico = 5;

        private readonly WishBoundContext _contexto;

        public BannersController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/banners?utilizadorId=N  (os que estão a decorrer; para um
        // ADMINISTRADOR também os eventos já terminados — o admin nunca perde
        // o acesso às personagens exclusivas)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BannerResposta>>> ObterBanners([FromQuery] int utilizadorId = 0)
        {
            try
            {
                var agora = DateTime.UtcNow;
                bool admin = await EhAdminAsync(utilizadorId);

                var banners = await _contexto.Banners.AsNoTracking()
                    .Where(b => b.IsAtivo && (admin || (b.DataInicio <= agora && b.DataFim >= agora)))
                    // Permanente primeiro, depois os eventos a decorrer (os que
                    // acabam mais cedo primeiro) e, para o admin, os terminados no fim
                    .OrderBy(b => b.TipoBanner == Banner.TipoStandard ? 0 : (b.DataFim >= agora ? 1 : 2))
                    .ThenBy(b => b.DataFim)
                    .ToListAsync();

                var idsPermanentes = await ObterIdsBannersPermanentesAsync();
                var resposta = new List<BannerResposta>();

                foreach (var banner in banners)
                {
                    var pool = await ObterPoolAsync(banner.Id, idsPermanentes, utilizadorId: 0);
                    var item = new BannerResposta();
                    PreencherResumo(item, banner, pool);
                    item.TemRecompensas = await _contexto.RecompensasEvento.AnyAsync(r => r.BannerId == banner.Id);
                    resposta.Add(item);
                }

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter os banners: " + ex.Message);
            }
        }

        // GET: api/banners/5?utilizadorId=3
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BannerDetalheResposta>> ObterBanner(int id, [FromQuery] int utilizadorId = 0)
        {
            try
            {
                var banner = await _contexto.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                if (banner == null || !banner.IsAtivo)
                {
                    return NotFound("O banner não existe.");
                }

                var idsPermanentes = await ObterIdsBannersPermanentesAsync();
                var pool = await ObterPoolAsync(banner.Id, idsPermanentes, utilizadorId);

                var detalhe = new BannerDetalheResposta();
                PreencherResumo(detalhe, banner, pool);
                detalhe.Personagens = pool;

                // Regras do rate-up em texto, uma por raridade com destaque
                foreach (var grupo in pool.Where(p => p.RateUp).GroupBy(p => p.RaridadeOrdem).OrderByDescending(g => g.Key))
                {
                    var destacadas = grupo.ToList();
                    decimal quota = destacadas.First().Quota ?? QuotaRateUpPorOmissao;
                    string nomes = string.Join(" ou ", destacadas.Select(d => d.Nome));
                    string raridade = destacadas.First().RaridadeNome;

                    if (grupo.Key >= OrdemMitico && quota == 0.5m)
                    {
                        detalhe.RegrasRateUp.Add(raridade + ": 50/50 — metade das vezes sai " + nomes +
                            ", a outra metade uma Mítica do banner permanente. Quem perde o 50/50 tem a próxima Mítica garantida " + nomes + ".");
                    }
                    else
                    {
                        detalhe.RegrasRateUp.Add(raridade + ": " + (quota * 100).ToString("0") + "% das vezes sai " + nomes +
                            ", " + ((1 - quota) * 100).ToString("0") + "% uma das restantes.");
                    }
                }

                // Recompensas diárias do evento (se as tiver)
                var recompensas = await _contexto.RecompensasEvento.AsNoTracking()
                    .Where(r => r.BannerId == banner.Id)
                    .ToListAsync();

                detalhe.TemRecompensas = recompensas.Count > 0;
                detalhe.DiasRecompensa = recompensas.Count;
                detalhe.TotalRecompensas = recompensas.Sum(r => r.QuantidadeMoeda ?? 0);

                if (recompensas.Count > 0)
                {
                    int? tipoMoedaId = recompensas.First().TipoMoedaId;
                    detalhe.MoedaRecompensa = tipoMoedaId == null
                        ? null
                        : await _contexto.TiposMoeda.Where(t => t.Id == tipoMoedaId).Select(t => t.Nome).FirstOrDefaultAsync();
                }

                // Participação do utilizador
                if (utilizadorId > 0)
                {
                    var pity = await _contexto.Pity.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.UtilizadorId == utilizadorId && p.BannerId == banner.Id);

                    detalhe.ContadorLendario = pity?.ContadorAtual ?? 0;
                    detalhe.ContadorEpico = pity?.ContadorEpico ?? 0;
                    detalhe.GarantiaRateUp = pity?.GarantiaRateUp ?? false;

                    detalhe.InvocacoesFeitas = await _contexto.Invocacoes
                        .CountAsync(i => i.UtilizadorId == utilizadorId && i.BannerId == banner.Id);

                    detalhe.PersonagensObtidas = pool.Count(p => p.Copias > 0);
                    detalhe.DestaquesObtidos = pool.Count(p => p.RateUp && p.Copias > 0);

                    if (recompensas.Count > 0)
                    {
                        detalhe.DiasResgatados = await _contexto.ParticipacoesEventos
                            .Where(p => p.UtilizadorId == utilizadorId && p.BannerId == banner.Id)
                            .Select(p => (int?)p.Progresso)
                            .FirstOrDefaultAsync() ?? 0;
                    }
                }

                return Ok(detalhe);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o banner: " + ex.Message);
            }
        }

        // ============================================================
        //  Métodos auxiliares
        // ============================================================

        /// <summary>Campos comuns à lista e ao detalhe.</summary>
        private static void PreencherResumo(BannerResposta destino, Banner banner, List<PersonagemDestaque> pool)
        {
            bool permanente = banner.TipoBanner == Banner.TipoStandard;

            destino.Id = banner.Id;
            destino.Nome = banner.Nome;
            destino.Descricao = banner.Descricao;
            destino.TipoBanner = banner.TipoBanner;
            destino.ImagemUrl = banner.ImagemUrl;
            destino.DataInicio = banner.DataInicio;
            destino.DataFim = banner.DataFim;
            destino.Permanente = permanente;
            destino.SegundosRestantes = permanente
                ? 0
                : (long)Math.Max(0, (banner.DataFim - DateTime.UtcNow).TotalSeconds);
            destino.Terminado = !permanente && !banner.ADecorrer;
            destino.TotalPersonagens = pool.Count;
            destino.TotalExclusivas = pool.Count(p => p.Exclusiva);
            destino.Destaques = pool.Where(p => p.RateUp).ToList();

            // Montra: as rate-up (se houver) ou as 3 mais raras; a mais rara ao centro
            var montra = destino.Destaques.Count > 0 ? destino.Destaques : pool;
            destino.Vitrine = montra
                .OrderByDescending(p => p.RaridadeOrdem)
                .ThenByDescending(p => p.RateUp)
                .ThenBy(p => p.Nome)
                .Take(3)
                .ToList();
        }

        /// <summary>
        /// Personagens ativas de um banner, da mais rara para a mais comum,
        /// com a marca de rate-up, de exclusiva e (com utilizadorId) as cópias
        /// que o utilizador já tem.
        /// </summary>
        private async Task<List<PersonagemDestaque>> ObterPoolAsync(int bannerId, HashSet<int> idsPermanentes, int utilizadorId)
        {
            var linhas = await _contexto.BannerPersonagens.AsNoTracking()
                .Where(bp => bp.BannerId == bannerId)
                .Join(_contexto.Personagens.Where(p => p.IsAtivo),
                      bp => bp.PersonagemId, p => p.Id,
                      (bp, p) => new { bp, p, r = p.Raridade! })
                .OrderByDescending(x => x.r.Ordem).ThenByDescending(x => x.bp.RateUp).ThenBy(x => x.p.Nome)
                .Select(x => new PersonagemDestaque
                {
                    Id = x.p.Id,
                    Nome = x.p.Nome,
                    Descricao = x.p.Descricao,
                    ImagemUrl = x.p.ImagemUrl,
                    RaridadeNome = x.r.Nome,
                    Cor = x.r.Cor,
                    RaridadeOrdem = x.r.Ordem,
                    RateUp = x.bp.RateUp,
                    Quota = x.bp.RateUp ? (decimal?)(x.bp.ProbabilidadeExtra ?? QuotaRateUpPorOmissao) : (decimal?)null
                })
                .ToListAsync();

            Dictionary<int, int> copias = utilizadorId > 0
                ? await _contexto.Colecoes.AsNoTracking()
                    .Where(c => c.UtilizadorId == utilizadorId)
                    .ToDictionaryAsync(c => c.PersonagemId, c => c.Quantidade)
                : new Dictionary<int, int>();

            foreach (var linha in linhas)
            {
                linha.Exclusiva = !idsPermanentes.Contains(linha.Id);
                linha.Copias = copias.TryGetValue(linha.Id, out int n) ? n : 0;
            }

            return linhas;
        }

        /// <summary>O utilizador indicado é administrador? (0 / inexistente = não)</summary>
        private async Task<bool> EhAdminAsync(int utilizadorId)
        {
            if (utilizadorId <= 0)
            {
                return false;
            }

            return await _contexto.Utilizadores.AsNoTracking()
                .Where(u => u.Id == utilizadorId && u.IsAtivo)
                .Select(u => u.IsAdmin)
                .FirstOrDefaultAsync();
        }

        /// <summary>Personagens que existem em algum banner permanente (as outras são exclusivas de evento).</summary>
        private async Task<HashSet<int>> ObterIdsBannersPermanentesAsync()
        {
            var ids = await _contexto.BannerPersonagens
                .Where(bp => _contexto.Banners.Any(b => b.Id == bp.BannerId && b.TipoBanner == Banner.TipoStandard))
                .Select(bp => bp.PersonagemId)
                .Distinct()
                .ToListAsync();

            return ids.ToHashSet();
        }
    }
}
