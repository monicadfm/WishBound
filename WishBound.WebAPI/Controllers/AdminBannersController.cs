using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// CRUD DE BANNERS E EVENTOS (api/admin/banners) — tudo o que a §8
    /// (eventos, rate-up, exclusivas, 50/50) lê da base de dados passa a
    /// poder ser gerido no painel, sem scripts:
    ///
    ///   GET    api/admin/banners?adminId=                    - lista com estado e contagens
    ///   GET    api/admin/banners/{id}?adminId=               - detalhe: pool, rate-up, recompensas
    ///   POST   api/admin/banners                             - criar (opcional: copiar a pool
    ///                                                          de outro banner, notificar todos)
    ///   PUT    api/admin/banners/{id}                        - editar nome, texto, imagem, datas, ativo
    ///   DELETE api/admin/banners/{id}?adminId=               - apagar (só se nunca foi usado)
    ///   POST   api/admin/banners/{id}/pool                   - substitui a pool + rate-up + quotas
    ///   POST   api/admin/banners/{id}/recompensas            - acrescenta um dia de recompensa
    ///   PUT    api/admin/banners/{id}/recompensas/{rid}      - edita um dia
    ///   DELETE api/admin/banners/{id}/recompensas/{rid}      - apaga o ÚLTIMO dia
    ///
    /// REGRAS (as mesmas que o InvocacoesController e o EconomiaController assumem)
    ///   - O banner permanente (Id 1) nunca é desativado nem muda de tipo.
    ///   - O tipo (Standard/Evento) só muda enquanto o banner não tiver
    ///     invocações — é ele que decide quem é "exclusiva".
    ///   - Rate-up só em banners de Evento; a quota é POR RARIDADE
    ///     (0.80 = 80% das Lendárias que saem são as destacadas; na Mítica é
    ///     o 50/50) e fica em BannerPersonagens.ProbabilidadeExtra de cada
    ///     destacada.
    ///   - Um banner ativo tem de ter pelo menos uma personagem.
    ///   - Recompensas de evento: uma linha por dia, pela ordem do Id; por
    ///     isso só se apaga o último dia, e só se ninguém já o recebeu.
    ///   - Apagar um banner só se não houver invocações, pity nem
    ///     participações — senão desativa-se (o histórico fica).
    /// Cada escrita fica em LogsAdministrador (categoria "Banner").
    /// </summary>
    [Route("api/admin/banners")]
    [ApiController]
    public class AdminBannersController : AdminBaseController
    {
        private const int IdBannerPermanente = 1;
        private const decimal QuotaMinima = 0.01m;
        private const decimal QuotaMaxima = 0.99m;
        private const decimal QuantidadeMaximaRecompensa = 100000m;

        public AdminBannersController(WishBoundContext contexto) : base(contexto)
        {
        }

        // ============================================================
        //  CONSULTAS
        // ============================================================

        // GET: api/admin/banners?adminId=1
        [HttpGet]
        public async Task<ActionResult<List<AdminBannerResumo>>> Obter([FromQuery] int adminId)
        {
            try
            {
                if (await ObterAdminAsync(adminId) == null)
                {
                    return ApenasAdmins("gerir os banners");
                }

                return Ok(await ObterResumosAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter os banners: " + ex.Message);
            }
        }

        // GET: api/admin/banners/5?adminId=1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdminBannerDetalhe>> ObterUm(int id, [FromQuery] int adminId)
        {
            try
            {
                if (await ObterAdminAsync(adminId) == null)
                {
                    return ApenasAdmins("gerir os banners");
                }

                var resumo = (await ObterResumosAsync(id)).FirstOrDefault();
                if (resumo == null)
                {
                    return NotFound("Banner não encontrado.");
                }

                var detalhe = new AdminBannerDetalhe { Banner = resumo };

                // ----- Pool: todas as personagens, marcando as que estão no banner -----
                var ligacoes = await _contexto.BannerPersonagens.AsNoTracking()
                    .Where(bp => bp.BannerId == id)
                    .ToDictionaryAsync(bp => bp.PersonagemId);

                var idsStandard = await _contexto.BannerPersonagens.AsNoTracking()
                    .Join(_contexto.Banners.Where(b => b.TipoBanner == Banner.TipoStandard),
                          bp => bp.BannerId, b => b.Id, (bp, b) => bp.PersonagemId)
                    .Distinct()
                    .ToListAsync();
                var standard = idsStandard.ToHashSet();

                var personagens = await _contexto.Personagens.AsNoTracking()
                    .Include(p => p.Raridade)
                    .OrderByDescending(p => p.Raridade!.Ordem).ThenBy(p => p.Nome)
                    .ToListAsync();

                foreach (var p in personagens)
                {
                    ligacoes.TryGetValue(p.Id, out var ligacao);

                    detalhe.Pool.Add(new AdminBannerPersonagem
                    {
                        PersonagemId = p.Id,
                        Nome = p.Nome,
                        ImagemUrl = p.ImagemUrl,
                        RaridadeId = p.RaridadeId,
                        RaridadeNome = p.Raridade?.Nome ?? "-",
                        RaridadeCor = p.Raridade?.Cor,
                        RaridadeOrdem = p.Raridade?.Ordem ?? 0,
                        IsAtivo = p.IsAtivo,
                        NoBanner = ligacao != null,
                        RateUp = ligacao?.RateUp ?? false,
                        Quota = ligacao?.ProbabilidadeExtra,
                        EmStandard = standard.Contains(p.Id)
                    });

                    if (ligacao != null && ligacao.RateUp && ligacao.ProbabilidadeExtra.HasValue &&
                        !detalhe.QuotasPorRaridade.ContainsKey(p.RaridadeId))
                    {
                        detalhe.QuotasPorRaridade[p.RaridadeId] = ligacao.ProbabilidadeExtra.Value;
                    }
                }

                // ----- Recompensas do evento (um dia por linha, pela ordem do Id) -----
                var nomesMoeda = await _contexto.TiposMoeda.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
                var progresso = await _contexto.ParticipacoesEventos.AsNoTracking()
                    .Where(p => p.BannerId == id)
                    .Select(p => p.Progresso)
                    .ToListAsync();

                var recompensas = await _contexto.RecompensasEvento.AsNoTracking()
                    .Where(r => r.BannerId == id)
                    .OrderBy(r => r.Id)
                    .ToListAsync();

                int dia = 0;
                foreach (var r in recompensas)
                {
                    dia++;
                    detalhe.RecompensasEvento.Add(new AdminRecompensaEvento
                    {
                        Id = r.Id,
                        Dia = dia,
                        Descricao = r.Descricao,
                        TipoMoedaId = r.TipoMoedaId,
                        MoedaNome = nomesMoeda.FirstOrDefault(t => t.Id == r.TipoMoedaId)?.Nome,
                        Quantidade = r.QuantidadeMoeda,
                        JaRecebido = progresso.Count(p => p >= dia)
                    });
                }

                detalhe.TiposMoeda = nomesMoeda.Select(t => new AdminOpcao { Id = t.Id, Nome = t.Nome }).ToList();

                detalhe.Raridades = await _contexto.Raridades.AsNoTracking()
                    .OrderByDescending(r => r.Ordem)
                    .Select(r => new AdminOpcao { Id = r.Id, Nome = r.Nome, Cor = r.Cor, Ordem = r.Ordem })
                    .ToListAsync();

                detalhe.OutrosBanners = await _contexto.Banners.AsNoTracking()
                    .Where(b => b.Id != id)
                    .OrderBy(b => b.Id)
                    .Select(b => new AdminOpcao { Id = b.Id, Nome = b.Nome })
                    .ToListAsync();

                return Ok(detalhe);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o banner: " + ex.Message);
            }
        }

        // ============================================================
        //  BANNER
        // ============================================================

        // POST: api/admin/banners
        [HttpPost]
        public async Task<ActionResult<AdminAcaoResultado>> Criar([FromBody] AdminBannerPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir os banners");
                }

                string? erro = ValidarBanner(pedido);
                if (erro != null)
                {
                    return BadRequest(erro);
                }

                Banner? origem = null;
                if (pedido.CopiarPoolDe.HasValue && pedido.CopiarPoolDe.Value > 0)
                {
                    origem = await _contexto.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == pedido.CopiarPoolDe.Value);
                    if (origem == null)
                    {
                        return BadRequest("O banner de onde copiar a pool não existe.");
                    }
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                var banner = new Banner
                {
                    Nome = pedido.Nome.Trim(),
                    Descricao = string.IsNullOrWhiteSpace(pedido.Descricao) ? null : pedido.Descricao.Trim(),
                    TipoBanner = pedido.TipoBanner,
                    ImagemUrl = string.IsNullOrWhiteSpace(pedido.ImagemUrl) ? null : pedido.ImagemUrl.Trim(),
                    DataInicio = ParaUtc(pedido.DataInicio),
                    DataFim = ParaUtc(pedido.DataFim),
                    IsAtivo = pedido.IsAtivo
                };

                _contexto.Banners.Add(banner);
                await _contexto.SaveChangesAsync();

                int copiadas = 0;
                if (origem != null)
                {
                    var linhas = await _contexto.BannerPersonagens.AsNoTracking()
                        .Where(bp => bp.BannerId == origem.Id)
                        .ToListAsync();

                    bool evento = banner.TipoBanner == Banner.TipoEvento;
                    foreach (var l in linhas)
                    {
                        _contexto.BannerPersonagens.Add(new BannerPersonagem
                        {
                            BannerId = banner.Id,
                            PersonagemId = l.PersonagemId,
                            RateUp = evento && l.RateUp,
                            ProbabilidadeExtra = evento && l.RateUp ? l.ProbabilidadeExtra : null
                        });
                    }
                    copiadas = linhas.Count;
                }

                // Sem personagens não se pode invocar: fica inativo até ter pool
                string aviso = string.Empty;
                if (copiadas == 0 && banner.IsAtivo)
                {
                    banner.IsAtivo = false;
                    aviso = " Ficou inativo até lhe juntar personagens.";
                }

                int notificadas = 0;
                if (pedido.Notificar && banner.IsAtivo)
                {
                    notificadas = await NotificarBannerAsync(banner);
                }

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoBanner + ": criado " + banner.Nome,
                    "Banners", banner.Id,
                    ComMotivo(banner.TipoBanner + ", " + Datas(banner) +
                              (origem != null ? ", pool copiada de \"" + origem.Nome + "\" (" + copiadas + " personagens)" : ", sem personagens") +
                              (notificadas > 0 ? ", " + notificadas + " contas notificadas" : "") + ".", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = "Banner \"" + banner.Nome + "\" criado." + aviso +
                               (notificadas > 0 ? " " + notificadas + " contas notificadas." : ""),
                    NovoValor = banner.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao criar o banner: " + ex.Message);
            }
        }

        // PUT: api/admin/banners/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<AdminAcaoResultado>> Editar(int id, [FromBody] AdminBannerPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir os banners");
                }

                var banner = await _contexto.Banners.FirstOrDefaultAsync(b => b.Id == id);
                if (banner == null)
                {
                    return NotFound("Banner não encontrado.");
                }

                string? erro = ValidarBanner(pedido);
                if (erro != null)
                {
                    return BadRequest(erro);
                }

                // ----- Proteções -----
                if (id == IdBannerPermanente && (!pedido.IsAtivo || pedido.TipoBanner != Banner.TipoStandard))
                {
                    return BadRequest("O banner permanente não pode ser desativado nem mudar de tipo.");
                }

                if (pedido.TipoBanner != banner.TipoBanner &&
                    await _contexto.Invocacoes.AnyAsync(i => i.BannerId == id))
                {
                    return BadRequest("Este banner já tem invocações — o tipo (Standard/Evento) já não pode mudar.");
                }

                bool temPool = await _contexto.BannerPersonagens.AnyAsync(bp => bp.BannerId == id);
                if (pedido.IsAtivo && !temPool)
                {
                    return BadRequest("Junte personagens ao banner antes de o ativar.");
                }

                // ----- Alterações -----
                var mudancas = new List<string>();
                var inicio = ParaUtc(pedido.DataInicio);
                var fim = ParaUtc(pedido.DataFim);
                string nome = pedido.Nome.Trim();
                string? descricao = string.IsNullOrWhiteSpace(pedido.Descricao) ? null : pedido.Descricao.Trim();
                string? imagem = string.IsNullOrWhiteSpace(pedido.ImagemUrl) ? null : pedido.ImagemUrl.Trim();

                if (nome != banner.Nome) mudancas.Add("nome \"" + banner.Nome + "\" → \"" + nome + "\"");
                if (descricao != banner.Descricao) mudancas.Add("descrição");
                if (imagem != banner.ImagemUrl) mudancas.Add("imagem");
                if (pedido.TipoBanner != banner.TipoBanner) mudancas.Add("tipo " + banner.TipoBanner + " → " + pedido.TipoBanner);
                if (inicio != banner.DataInicio || fim != banner.DataFim) mudancas.Add("datas " + Datas(inicio, fim));
                if (pedido.IsAtivo != banner.IsAtivo) mudancas.Add(pedido.IsAtivo ? "ativado" : "desativado");

                if (mudancas.Count == 0)
                {
                    return Ok(new AdminAcaoResultado { Mensagem = "Nada foi alterado." });
                }

                bool passouAAtivo = pedido.IsAtivo && !banner.IsAtivo;

                // Alterações, limpeza do rate-up, notificações e registo: tudo ou nada
                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                banner.Nome = nome;
                banner.Descricao = descricao;
                banner.ImagemUrl = imagem;
                banner.TipoBanner = pedido.TipoBanner;
                banner.DataInicio = inicio;
                banner.DataFim = fim;
                banner.IsAtivo = pedido.IsAtivo;

                // Um banner que passa a Standard deixa de ter rate-up
                if (banner.TipoBanner == Banner.TipoStandard)
                {
                    await _contexto.BannerPersonagens
                        .Where(bp => bp.BannerId == id && bp.RateUp)
                        .ExecuteUpdateAsync(s => s.SetProperty(bp => bp.RateUp, false).SetProperty(bp => bp.ProbabilidadeExtra, (decimal?)null));
                }

                int notificadas = 0;
                if (pedido.Notificar && banner.IsAtivo)
                {
                    notificadas = await NotificarBannerAsync(banner);
                }

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoBanner + ": editado " + banner.Nome,
                    "Banners", banner.Id,
                    ComMotivo(Capitalizar(string.Join(", ", mudancas)) +
                              (notificadas > 0 ? "; " + notificadas + " contas notificadas" : "") + ".", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = "Banner \"" + banner.Nome + "\" atualizado." +
                               (passouAAtivo && banner.DataFim < DateTime.UtcNow ? " Atenção: as datas já passaram — só os administradores o veem." : "") +
                               (notificadas > 0 ? " " + notificadas + " contas notificadas." : "")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao atualizar o banner: " + ex.Message);
            }
        }

        // DELETE: api/admin/banners/5?adminId=1&motivo=...
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<AdminAcaoResultado>> Apagar(int id, [FromQuery] int adminId, [FromQuery] string? motivo)
        {
            try
            {
                var admin = await ObterAdminAsync(adminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir os banners");
                }

                var banner = await _contexto.Banners.FirstOrDefaultAsync(b => b.Id == id);
                if (banner == null)
                {
                    return NotFound("Banner não encontrado.");
                }

                if (id == IdBannerPermanente)
                {
                    return BadRequest("O banner permanente não pode ser apagado.");
                }

                if (await _contexto.Invocacoes.AnyAsync(i => i.BannerId == id) ||
                    await _contexto.Pity.AnyAsync(p => p.BannerId == id) ||
                    await _contexto.ParticipacoesEventos.AnyAsync(p => p.BannerId == id))
                {
                    return BadRequest("Este banner já foi usado (invocações, pity ou recompensas recebidas) — desative-o em vez de o apagar, para o histórico ficar.");
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                int pool = await _contexto.BannerPersonagens.Where(bp => bp.BannerId == id).ExecuteDeleteAsync();
                int dias = await _contexto.RecompensasEvento.Where(r => r.BannerId == id).ExecuteDeleteAsync();

                _contexto.Banners.Remove(banner);
                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoBanner + ": apagado " + banner.Nome,
                    "Banners", banner.Id,
                    ComMotivo(banner.TipoBanner + ", " + pool + " personagens, " + dias + " dias de recompensa.", motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Banner \"" + banner.Nome + "\" apagado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao apagar o banner: " + ex.Message);
            }
        }

        // ============================================================
        //  POOL + RATE-UP
        // ============================================================

        // POST: api/admin/banners/5/pool
        [HttpPost("{id:int}/pool")]
        public async Task<ActionResult<AdminAcaoResultado>> DefinirPool(int id, [FromBody] AdminBannerPoolPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir os banners");
                }

                var banner = await _contexto.Banners.FirstOrDefaultAsync(b => b.Id == id);
                if (banner == null)
                {
                    return NotFound("Banner não encontrado.");
                }

                var itens = (pedido.Itens ?? new List<AdminPoolItem>())
                    .GroupBy(i => i.PersonagemId)
                    .Select(g => g.First())
                    .ToList();

                if (itens.Count == 0 && banner.IsAtivo)
                {
                    return BadRequest("Um banner ativo tem de ter pelo menos uma personagem (desative-o primeiro para o esvaziar).");
                }

                var ids = itens.Select(i => i.PersonagemId).ToList();
                var personagens = await _contexto.Personagens.AsNoTracking()
                    .Include(p => p.Raridade)
                    .Where(p => ids.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                if (personagens.Count != ids.Count)
                {
                    return BadRequest("Uma das personagens escolhidas não existe.");
                }

                bool evento = banner.TipoBanner == Banner.TipoEvento;
                var destacadas = itens.Where(i => i.RateUp).ToList();

                if (!evento && destacadas.Count > 0)
                {
                    return BadRequest("O rate-up só existe em banners de Evento — o permanente sorteia sempre ao acaso dentro da raridade.");
                }

                // Quota por raridade: obrigatória para cada raridade com destacadas
                var quotas = pedido.QuotasPorRaridade ?? new Dictionary<int, decimal>();
                foreach (var raridadeId in destacadas.Select(d => personagens[d.PersonagemId].RaridadeId).Distinct())
                {
                    if (!quotas.TryGetValue(raridadeId, out var quota) || quota < QuotaMinima || quota > QuotaMaxima)
                    {
                        var nomeRaridade = personagens.Values.First(p => p.RaridadeId == raridadeId).Raridade?.Nome ?? "#" + raridadeId;
                        return BadRequest("Indique a quota do rate-up da raridade " + nomeRaridade + " (entre 1% e 99%).");
                    }
                }

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                await _contexto.BannerPersonagens.Where(bp => bp.BannerId == id).ExecuteDeleteAsync();

                foreach (var item in itens)
                {
                    int raridadeId = personagens[item.PersonagemId].RaridadeId;
                    _contexto.BannerPersonagens.Add(new BannerPersonagem
                    {
                        BannerId = id,
                        PersonagemId = item.PersonagemId,
                        RateUp = item.RateUp,
                        ProbabilidadeExtra = item.RateUp ? Math.Round(quotas[raridadeId], 4) : null
                    });
                }

                string resumoQuotas = string.Join(", ", destacadas
                    .GroupBy(d => personagens[d.PersonagemId].RaridadeId)
                    .Select(g => (personagens[g.First().PersonagemId].Raridade?.Nome ?? "?") + " " +
                                 (quotas[g.Key] * 100m).ToString("0.##", CultureInfo.GetCultureInfo("pt-PT")) + "% (" +
                                 string.Join("/", g.Select(d => personagens[d.PersonagemId].Nome)) + ")"));

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoBanner + ": pool de " + banner.Nome,
                    "BannerPersonagens", banner.Id,
                    ComMotivo(itens.Count + " personagens" +
                              (destacadas.Count > 0 ? "; rate-up: " + resumoQuotas : "; sem rate-up") + ".", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = "Pool de \"" + banner.Nome + "\" guardada: " + itens.Count + " personagens, " + destacadas.Count + " em destaque.",
                    NovoValor = itens.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao guardar a pool: " + ex.Message);
            }
        }

        // ============================================================
        //  RECOMPENSAS DO EVENTO (um dia por linha)
        // ============================================================

        // POST: api/admin/banners/5/recompensas
        [HttpPost("{id:int}/recompensas")]
        public async Task<ActionResult<AdminAcaoResultado>> AcrescentarDia(int id, [FromBody] AdminRecompensaEventoPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir os banners");
                }

                var banner = await _contexto.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                if (banner == null)
                {
                    return NotFound("Banner não encontrado.");
                }

                if (banner.TipoBanner != Banner.TipoEvento)
                {
                    return BadRequest("Só os banners de Evento têm recompensas diárias.");
                }

                var (moeda, erro) = await ValidarRecompensaAsync(pedido);
                if (erro != null)
                {
                    return BadRequest(erro);
                }

                int dia = await _contexto.RecompensasEvento.CountAsync(r => r.BannerId == id) + 1;

                var recompensa = new RecompensaEvento
                {
                    BannerId = id,
                    Descricao = DescricaoDia(dia, pedido, moeda!),
                    TipoMoedaId = moeda!.Id,
                    QuantidadeMoeda = pedido.Quantidade,
                    MetaNecessaria = dia.ToString(CultureInfo.InvariantCulture)
                };

                _contexto.RecompensasEvento.Add(recompensa);
                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoBanner + ": dia " + dia + " de " + banner.Nome,
                    "RecompensasEvento", id,
                    ComMotivo("Acrescentado: " + recompensa.Descricao + ".", pedido.Motivo));

                await _contexto.SaveChangesAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Dia " + dia + " acrescentado a \"" + banner.Nome + "\"." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao acrescentar a recompensa: " + ex.Message);
            }
        }

        // PUT: api/admin/banners/5/recompensas/12
        [HttpPut("{id:int}/recompensas/{recompensaId:int}")]
        public async Task<ActionResult<AdminAcaoResultado>> EditarDia(int id, int recompensaId, [FromBody] AdminRecompensaEventoPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir os banners");
                }

                var recompensa = await _contexto.RecompensasEvento.FirstOrDefaultAsync(r => r.Id == recompensaId && r.BannerId == id);
                if (recompensa == null)
                {
                    return NotFound("Recompensa não encontrada.");
                }

                var (moeda, erro) = await ValidarRecompensaAsync(pedido);
                if (erro != null)
                {
                    return BadRequest(erro);
                }

                int dia = await _contexto.RecompensasEvento.CountAsync(r => r.BannerId == id && r.Id <= recompensaId);
                string antes = recompensa.Descricao;

                recompensa.TipoMoedaId = moeda!.Id;
                recompensa.QuantidadeMoeda = pedido.Quantidade;
                recompensa.Descricao = DescricaoDia(dia, pedido, moeda);

                var nomeBanner = await _contexto.Banners.Where(b => b.Id == id).Select(b => b.Nome).FirstOrDefaultAsync() ?? "#" + id;

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoBanner + ": dia " + dia + " de " + nomeBanner,
                    "RecompensasEvento", recompensa.Id,
                    ComMotivo("\"" + antes + "\" → \"" + recompensa.Descricao + "\".", pedido.Motivo));

                await _contexto.SaveChangesAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Dia " + dia + " atualizado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao atualizar a recompensa: " + ex.Message);
            }
        }

        // DELETE: api/admin/banners/5/recompensas/12?adminId=1
        [HttpDelete("{id:int}/recompensas/{recompensaId:int}")]
        public async Task<ActionResult<AdminAcaoResultado>> ApagarDia(int id, int recompensaId, [FromQuery] int adminId, [FromQuery] string? motivo)
        {
            try
            {
                var admin = await ObterAdminAsync(adminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir os banners");
                }

                var dias = await _contexto.RecompensasEvento.Where(r => r.BannerId == id).OrderBy(r => r.Id).ToListAsync();
                var recompensa = dias.FirstOrDefault(r => r.Id == recompensaId);

                if (recompensa == null)
                {
                    return NotFound("Recompensa não encontrada.");
                }

                if (recompensa.Id != dias.Last().Id)
                {
                    return BadRequest("Só o último dia pode ser apagado (os dias seguem a ordem das linhas).");
                }

                int dia = dias.Count;
                if (await _contexto.ParticipacoesEventos.AnyAsync(p => p.BannerId == id && p.Progresso >= dia))
                {
                    return BadRequest("Já há contas que receberam o dia " + dia + " — não pode ser apagado.");
                }

                var nomeBanner = await _contexto.Banners.Where(b => b.Id == id).Select(b => b.Nome).FirstOrDefaultAsync() ?? "#" + id;

                _contexto.RecompensasEvento.Remove(recompensa);
                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoBanner + ": dia " + dia + " de " + nomeBanner,
                    "RecompensasEvento", recompensa.Id,
                    ComMotivo("Apagado: " + recompensa.Descricao + ".", motivo));

                await _contexto.SaveChangesAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Dia " + dia + " apagado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao apagar a recompensa: " + ex.Message);
            }
        }

        // ============================================================
        //  Auxiliares
        // ============================================================

        /// <summary>Resumo de todos os banners (ou de um só) com estado e contagens.</summary>
        private async Task<List<AdminBannerResumo>> ObterResumosAsync(int? apenasId = null)
        {
            var agora = DateTime.UtcNow;

            IQueryable<Banner> consulta = _contexto.Banners.AsNoTracking();
            if (apenasId.HasValue)
            {
                consulta = consulta.Where(b => b.Id == apenasId.Value);
            }

            var banners = await consulta.ToListAsync();
            var ids = banners.Select(b => b.Id).ToList();

            var pool = await _contexto.BannerPersonagens.AsNoTracking()
                .Where(bp => ids.Contains(bp.BannerId))
                .GroupBy(bp => bp.BannerId)
                .Select(g => new { BannerId = g.Key, Total = g.Count(), RateUp = g.Count(x => x.RateUp) })
                .ToListAsync();

            var recompensas = await _contexto.RecompensasEvento.AsNoTracking()
                .Where(r => ids.Contains(r.BannerId))
                .GroupBy(r => r.BannerId)
                .Select(g => new { BannerId = g.Key, Total = g.Count() })
                .ToListAsync();

            var invocacoes = await _contexto.Invocacoes.AsNoTracking()
                .Where(i => ids.Contains(i.BannerId))
                .GroupBy(i => i.BannerId)
                .Select(g => new { BannerId = g.Key, Total = g.Count() })
                .ToListAsync();

            var participantes = await _contexto.ParticipacoesEventos.AsNoTracking()
                .Where(p => ids.Contains(p.BannerId))
                .GroupBy(p => p.BannerId)
                .Select(g => new { BannerId = g.Key, Total = g.Count() })
                .ToListAsync();

            var comPity = await _contexto.Pity.AsNoTracking()
                .Where(p => ids.Contains(p.BannerId))
                .Select(p => p.BannerId)
                .Distinct()
                .ToListAsync();

            return banners
                .Select(b =>
                {
                    var p = pool.FirstOrDefault(x => x.BannerId == b.Id);
                    int nInvocacoes = invocacoes.FirstOrDefault(x => x.BannerId == b.Id)?.Total ?? 0;
                    int nParticipantes = participantes.FirstOrDefault(x => x.BannerId == b.Id)?.Total ?? 0;
                    string estado = EstadoBanner(b, agora);

                    return new AdminBannerResumo
                    {
                        Id = b.Id,
                        Nome = b.Nome,
                        Descricao = b.Descricao,
                        TipoBanner = b.TipoBanner,
                        ImagemUrl = b.ImagemUrl,
                        DataInicio = b.DataInicio,
                        DataFim = b.DataFim,
                        IsAtivo = b.IsAtivo,
                        Estado = estado,
                        EhPermanente = b.Id == IdBannerPermanente,
                        SegundosRestantes = estado == "A decorrer" ? (long)Math.Max(0, (b.DataFim - agora).TotalSeconds) : 0,
                        Personagens = p?.Total ?? 0,
                        RateUp = p?.RateUp ?? 0,
                        Recompensas = recompensas.FirstOrDefault(x => x.BannerId == b.Id)?.Total ?? 0,
                        Invocacoes = nInvocacoes,
                        Participantes = nParticipantes,
                        PodeApagar = b.Id != IdBannerPermanente && nInvocacoes == 0 && nParticipantes == 0 && !comPity.Contains(b.Id)
                    };
                })
                // Permanente, depois a decorrer, agendados, terminados, inativos
                .OrderBy(b => b.EhPermanente ? 0 : b.Estado switch { "A decorrer" => 1, "Agendado" => 2, "Terminado" => 3, _ => 4 })
                .ThenByDescending(b => b.DataInicio)
                .ToList();
        }

        private static string? ValidarBanner(AdminBannerPedido pedido)
        {
            string nome = (pedido.Nome ?? string.Empty).Trim();

            if (nome.Length < 3 || nome.Length > 100)
            {
                return "O nome deve ter entre 3 e 100 caracteres.";
            }

            if (pedido.TipoBanner != Banner.TipoStandard && pedido.TipoBanner != Banner.TipoEvento)
            {
                return "O tipo tem de ser Standard ou Evento.";
            }

            if (pedido.DataInicio == default || pedido.DataFim == default)
            {
                return "Indique as datas de início e de fim.";
            }

            if (pedido.DataFim <= pedido.DataInicio)
            {
                return "A data de fim tem de ser depois da data de início.";
            }

            if (pedido.ImagemUrl != null && pedido.ImagemUrl.Trim().Length > 255)
            {
                return "O caminho da imagem não pode ter mais de 255 caracteres.";
            }

            return null;
        }

        private async Task<(TipoMoeda? Moeda, string? Erro)> ValidarRecompensaAsync(AdminRecompensaEventoPedido pedido)
        {
            var moeda = await _contexto.TiposMoeda.AsNoTracking().FirstOrDefaultAsync(t => t.Id == pedido.TipoMoedaId);
            if (moeda == null)
            {
                return (null, "Escolha o tipo de moeda da recompensa.");
            }

            if (pedido.Quantidade <= 0 || pedido.Quantidade > QuantidadeMaximaRecompensa || decimal.Round(pedido.Quantidade, 2) != pedido.Quantidade)
            {
                return (null, "A quantidade deve ser maior do que zero (no máximo 100 000, até 2 casas decimais).");
            }

            if (pedido.Descricao != null && pedido.Descricao.Trim().Length > 255)
            {
                return (null, "A descrição não pode ter mais de 255 caracteres.");
            }

            return (moeda, null);
        }

        private static string DescricaoDia(int dia, AdminRecompensaEventoPedido pedido, TipoMoeda moeda)
        {
            if (!string.IsNullOrWhiteSpace(pedido.Descricao))
            {
                return pedido.Descricao.Trim();
            }

            string quantidade = pedido.Quantidade.ToString("0.##", CultureInfo.GetCultureInfo("pt-PT"));
            return "Dia " + dia + " - " + quantidade + " " + moeda.Nome;
        }

        /// <summary>Notificação "Evento" (ou "Banner" para um Standard) a todas as contas ativas.</summary>
        private Task<int> NotificarBannerAsync(Banner banner)
        {
            bool evento = banner.TipoBanner == Banner.TipoEvento;
            string tipo = evento ? "Evento" : "Banner";
            string titulo = (evento ? "Novo evento: " : "Novo banner: ") + banner.Nome;

            string quando = banner.DataInicio > DateTime.UtcNow
                ? "Começa a " + banner.DataInicio.ToLocalTime().ToString("dd/MM 'às' HH:mm") + "."
                : "Já está a decorrer, até " + banner.DataFim.ToLocalTime().ToString("dd/MM 'às' HH:mm") + ".";

            return NotificarTodosAsync(tipo, titulo, quando + " Vai à Invocação!");
        }

        /// <summary>O site envia horas de Portugal (Kind Local/Unspecified); a BD guarda UTC.</summary>
        private static DateTime ParaUtc(DateTime data) => data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Local).ToUniversalTime()
        };

        private static string Datas(Banner b) => Datas(b.DataInicio, b.DataFim);

        private static string Datas(DateTime inicio, DateTime fim) =>
            inicio.ToLocalTime().ToString("dd/MM/yyyy HH:mm") + " – " + fim.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

        private static string Capitalizar(string texto) =>
            string.IsNullOrEmpty(texto) ? texto : char.ToUpperInvariant(texto[0]) + texto.Substring(1);
    }
}
