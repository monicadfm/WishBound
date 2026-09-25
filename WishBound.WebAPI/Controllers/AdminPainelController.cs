using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// PAINEL DA ADMINISTRAÇÃO (a primeira página da Gestão).
    ///
    ///   GET api/admin/painel?adminId=   - números do dia (contas, invocações, moeda em
    ///                                     circulação, notificações por ler), banners
    ///                                     ativos, invocações dos últimos 14 dias,
    ///                                     últimas ações e ALERTAS
    ///
    /// Os alertas apontam coisas que precisam de atenção e dizem ao site que
    /// página as resolve (Raridades, Banner, Banners, Personagens,
    /// Utilizadores): probabilidades que não somam 100%, banner ativo sem
    /// personagens, evento a acabar ou sem recompensas, personagens fora de
    /// todos os banners a decorrer, contas com o email por validar.
    /// </summary>
    [Route("api/admin")]
    [ApiController]
    public class AdminPainelController : AdminBaseController
    {
        private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-PT");

        public AdminPainelController(WishBoundContext contexto) : base(contexto)
        {
        }

        // ============================================================
        //  PAINEL
        // ============================================================

        // GET: api/admin/painel?adminId=1
        [HttpGet("painel")]
        public async Task<ActionResult<AdminPainelResposta>> Painel([FromQuery] int adminId)
        {
            try
            {
                if (await ObterAdminAsync(adminId) == null)
                {
                    return ApenasAdmins("ver o painel");
                }

                var agora = DateTime.UtcNow;
                var hoje = agora.Date;
                var semana = agora.AddDays(-7);

                var painel = new AdminPainelResposta
                {
                    ContasTotal = await _contexto.Utilizadores.CountAsync(u => u.NomeUtilizador != "Sistema"),
                    ContasAtivas = await _contexto.Utilizadores.CountAsync(u => u.IsAtivo && u.NomeUtilizador != "Sistema"),
                    NovasContas7Dias = await _contexto.Utilizadores.CountAsync(u => u.DataCriacao >= semana),
                    ComLogin7Dias = await _contexto.Utilizadores.CountAsync(u => u.UltimoLogin >= semana),
                    InvocacoesHoje = await _contexto.Invocacoes.CountAsync(i => i.Data >= hoje),
                    Invocacoes7Dias = await _contexto.Invocacoes.CountAsync(i => i.Data >= semana),
                    MoedasEmCirculacao = await _contexto.Carteiras.Where(c => c.TipoMoedaId == TiposMoedaIds.Moedas).SumAsync(c => (decimal?)c.Saldo) ?? 0m,
                    BilhetesEmCirculacao = await _contexto.Carteiras.Where(c => c.TipoMoedaId == TiposMoedaIds.Bilhetes).SumAsync(c => (decimal?)c.Saldo) ?? 0m,
                    Personagens = await _contexto.Personagens.CountAsync(),
                    NotificacoesNaoLidas = await _contexto.Notificacoes.CountAsync(n => !n.IsLida),
                    UltimasAcoes = await ObterAcoesRecentesAsync(8),
                    InvocacoesUltimos14Dias = await InvocacoesPorDiaAsync(14)
                };

                // ----- Banners ativos (a decorrer e agendados) -----
                var banners = await _contexto.Banners.AsNoTracking()
                    .Where(b => b.IsAtivo && b.DataFim >= agora)
                    .OrderBy(b => b.Id == 1 ? 0 : 1).ThenBy(b => b.DataFim)
                    .ToListAsync();

                var idsBanners = banners.Select(b => b.Id).ToList();
                var poolPorBanner = await _contexto.BannerPersonagens.AsNoTracking()
                    .Where(bp => idsBanners.Contains(bp.BannerId))
                    .GroupBy(bp => bp.BannerId)
                    .Select(g => new { g.Key, Total = g.Count(), RateUp = g.Count(x => x.RateUp) })
                    .ToDictionaryAsync(x => x.Key);
                var diasPorBanner = await _contexto.RecompensasEvento.AsNoTracking()
                    .Where(r => idsBanners.Contains(r.BannerId))
                    .GroupBy(r => r.BannerId)
                    .Select(g => new { g.Key, Total = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Total);

                foreach (var b in banners)
                {
                    string estado = EstadoBanner(b, agora);
                    painel.Banners.Add(new AdminBannerResumo
                    {
                        Id = b.Id,
                        Nome = b.Nome,
                        TipoBanner = b.TipoBanner,
                        DataInicio = b.DataInicio,
                        DataFim = b.DataFim,
                        IsAtivo = b.IsAtivo,
                        Estado = estado,
                        EhPermanente = b.Id == 1,
                        SegundosRestantes = estado == "A decorrer" ? (long)Math.Max(0, (b.DataFim - agora).TotalSeconds) : 0,
                        Personagens = poolPorBanner.TryGetValue(b.Id, out var p) ? p.Total : 0,
                        RateUp = p?.RateUp ?? 0,
                        Recompensas = diasPorBanner.TryGetValue(b.Id, out var d) ? d : 0
                    });
                }

                painel.Alertas = await CalcularAlertasAsync(painel.Banners, agora);

                return Ok(painel);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o painel: " + ex.Message);
            }
        }

        /// <summary>Coisas que merecem atenção do administrador (cada uma leva à página que a resolve).</summary>
        private async Task<List<AdminAlerta>> CalcularAlertasAsync(List<AdminBannerResumo> banners, DateTime agora)
        {
            var alertas = new List<AdminAlerta>();

            decimal soma = await _contexto.Raridades.SumAsync(r => (decimal?)r.Probabilidade) ?? 0m;
            if (Math.Abs(soma - 1m) > 0.0001m)
            {
                alertas.Add(new AdminAlerta
                {
                    Nivel = "aviso",
                    Texto = "As probabilidades das raridades somam " + (soma * 100m).ToString("0.##", Pt) +
                            "% — o sorteio normaliza-as, mas a tabela pública de probabilidades não bate certo.",
                    Destino = "Raridades"
                });
            }

            foreach (var b in banners.Where(b => b.Personagens == 0))
            {
                alertas.Add(new AdminAlerta { Nivel = "aviso", Texto = "O banner \"" + b.Nome + "\" está ativo mas não tem personagens.", Destino = "Banner", DestinoId = b.Id });
            }

            foreach (var b in banners.Where(b => b.Estado == "A decorrer" && b.TipoBanner == Banner.TipoEvento))
            {
                if (b.SegundosRestantes < 48 * 3600)
                {
                    alertas.Add(new AdminAlerta { Nivel = "info", Texto = "O evento \"" + b.Nome + "\" termina em menos de 48 horas.", Destino = "Banner", DestinoId = b.Id });
                }

                if (b.Recompensas == 0)
                {
                    alertas.Add(new AdminAlerta { Nivel = "info", Texto = "O evento \"" + b.Nome + "\" não tem recompensas diárias.", Destino = "Banner", DestinoId = b.Id });
                }
            }

            if (!banners.Any(b => b.Estado == "A decorrer" && b.TipoBanner == Banner.TipoEvento))
            {
                alertas.Add(new AdminAlerta { Nivel = "info", Texto = "Não há nenhum evento a decorrer.", Destino = "Banners" });
            }

            var idsADecorrer = banners.Where(b => b.Estado == "A decorrer").Select(b => b.Id).ToList();
            int foraDeBanners = await _contexto.Personagens
                .CountAsync(p => p.IsAtivo && !_contexto.BannerPersonagens.Any(bp => bp.PersonagemId == p.Id && idsADecorrer.Contains(bp.BannerId)));
            if (foraDeBanners > 0)
            {
                alertas.Add(new AdminAlerta
                {
                    Nivel = "info",
                    Texto = foraDeBanners + (foraDeBanners == 1 ? " personagem ativa não sai" : " personagens ativas não saem") + " em nenhum banner a decorrer.",
                    Destino = "Banners"
                });
            }

            int semImagem = await _contexto.Personagens.CountAsync(p => p.ImagemUrl == null || p.ImagemUrl == "");
            if (semImagem > 0)
            {
                alertas.Add(new AdminAlerta { Nivel = "info", Texto = semImagem + " personagem(ns) sem imagem.", Destino = "Personagens" });
            }

            int porValidar = await _contexto.Utilizadores.CountAsync(u => u.IsAtivo && !u.EmailValidado && u.NomeUtilizador != "Sistema");
            if (porValidar > 0)
            {
                alertas.Add(new AdminAlerta { Nivel = "info", Texto = porValidar + " conta(s) ativa(s) com o email por validar.", Destino = "Utilizadores" });
            }

            return alertas;
        }
    }
}
