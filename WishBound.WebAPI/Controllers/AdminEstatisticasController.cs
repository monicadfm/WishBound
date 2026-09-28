using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;
using WishBound.WebAPI.Services;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// ESTATÍSTICAS E EXPORTAÇÕES da administração.
    ///
    ///   GET api/admin/estatisticas?adminId=&dias=30           - estatísticas da plataforma
    ///   GET api/admin/exportar?adminId=&conjunto=&formato=    - ficheiro PDF ou XML
    ///
    /// As estatísticas usam as TRÊS VISTAS criadas com a base de dados
    /// (vw_EstatisticasGerais, vw_DistribuicaoRaridades,
    /// vw_PersonagensMaisPopulares), lidas com Database.SqlQuery, e juntam
    /// contagens feitas pelo EF (invocações por dia, por banner, economia,
    /// níveis de amizade, ações de administração).
    ///
    /// Conjuntos exportáveis: estatisticas, utilizadores, personagens,
    /// banners, acoes (registo de ações) e transacoes (últimos 30 dias).
    /// Cada exportação fica no registo de ações (categoria "Exportacao").
    /// </summary>
    [Route("api/admin")]
    [ApiController]
    public class AdminEstatisticasController : AdminBaseController
    {
        public static readonly string[] Conjuntos = { "estatisticas", "utilizadores", "personagens", "banners", "acoes", "transacoes" };

        private static readonly CultureInfo Pt = ServicoExportacao.Pt;

        public AdminEstatisticasController(WishBoundContext contexto) : base(contexto)
        {
        }

        // ============================================================
        //  ESTATÍSTICAS
        // ============================================================

        // GET: api/admin/estatisticas?adminId=1&dias=30
        [HttpGet("estatisticas")]
        public async Task<ActionResult<AdminEstatisticasResposta>> Estatisticas([FromQuery] int adminId, [FromQuery] int dias = 30)
        {
            try
            {
                if (await ObterAdminAsync(adminId) == null)
                {
                    return ApenasAdmins("ver as estatísticas");
                }

                return Ok(await CalcularEstatisticasAsync(Math.Clamp(dias, 7, 365)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao calcular as estatísticas: " + ex.Message);
            }
        }

        private async Task<AdminEstatisticasResposta> CalcularEstatisticasAsync(int dias)
        {
            var agora = DateTime.UtcNow;
            var desde = agora.Date.AddDays(-(dias - 1));

            var r = new AdminEstatisticasResposta { Dias = dias, GeradoEm = agora };

            // ----- 1) vw_EstatisticasGerais -----
            var geral = (await _contexto.Database
                .SqlQuery<VistaEstatisticasGerais>($"SELECT TotalUtilizadores, TotalInvocacoes, TotalPersonagensObtidas FROM vw_EstatisticasGerais")
                .ToListAsync()).FirstOrDefault();

            r.UtilizadoresAtivos = geral?.TotalUtilizadores ?? 0;
            r.TotalInvocacoes = geral?.TotalInvocacoes ?? 0;
            r.TotalPersonagensObtidas = geral?.TotalPersonagensObtidas ?? 0;

            r.ContasTotal = await _contexto.Utilizadores.CountAsync(u => u.NomeUtilizador != "Sistema");
            r.NovasContas = await _contexto.Utilizadores.CountAsync(u => u.DataCriacao >= desde);
            r.ContasComLogin = await _contexto.Utilizadores.CountAsync(u => u.UltimoLogin >= desde);
            r.InvocacoesPeriodo = await _contexto.Invocacoes.CountAsync(i => i.Data >= desde);
            r.InvocacoesComPity = await _contexto.Invocacoes.CountAsync(i => i.Data >= desde && i.PityAtivado);

            r.InvocacoesPorDia = await InvocacoesPorDiaAsync(dias);

            var contasPorDia = await _contexto.Utilizadores.AsNoTracking()
                .Where(u => u.DataCriacao >= desde)
                .GroupBy(u => u.DataCriacao.Date)
                .Select(g => new { Dia = g.Key, Total = g.Count() })
                .ToListAsync();
            r.ContasPorDia = PreencherDias(desde, dias, contasPorDia.ToDictionary(x => x.Dia, x => x.Total));

            // ----- 2) vw_DistribuicaoRaridades (+ cor, escalão e % esperada da tabela Raridades) -----
            var distribuicao = await _contexto.Database
                .SqlQuery<VistaDistribuicaoRaridades>($"SELECT Raridade, TotalObtidas FROM vw_DistribuicaoRaridades")
                .ToListAsync();

            var raridades = await _contexto.Raridades.AsNoTracking().ToListAsync();
            decimal somaPesos = raridades.Sum(x => x.Probabilidade);
            int totalObtidas = distribuicao.Sum(d => d.TotalObtidas);

            r.DistribuicaoRaridades = distribuicao
                .Select(d =>
                {
                    var raridade = raridades.FirstOrDefault(x => x.Nome == d.Raridade);
                    return new AdminDistribuicaoRaridade
                    {
                        Raridade = d.Raridade,
                        Cor = raridade?.Cor,
                        Ordem = raridade?.Ordem ?? 0,
                        Obtidas = d.TotalObtidas,
                        Percentagem = totalObtidas > 0 ? Math.Round(100m * d.TotalObtidas / totalObtidas, 2) : 0m,
                        PercentagemEsperada = raridade != null && somaPesos > 0 ? Math.Round(100m * raridade.Probabilidade / somaPesos, 2) : 0m
                    };
                })
                .OrderBy(d => d.Ordem)
                .ToList();

            // ----- 3) vw_PersonagensMaisPopulares (+ raridade e total de cópias) -----
            var populares = await _contexto.Database
                .SqlQuery<VistaPersonagensMaisPopulares>($"SELECT PersonagemId, Nome, TotalObtencoes FROM vw_PersonagensMaisPopulares")
                .ToListAsync();

            var personagens = await _contexto.Personagens.AsNoTracking().Include(p => p.Raridade).ToDictionaryAsync(p => p.Id);
            var copias = await _contexto.Colecoes.AsNoTracking()
                .GroupBy(c => c.PersonagemId)
                .Select(g => new { g.Key, Total = g.Sum(c => c.Quantidade) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            r.PersonagensPopulares = populares
                .OrderByDescending(p => p.TotalObtencoes).ThenBy(p => p.Nome)
                .Select(p =>
                {
                    personagens.TryGetValue(p.PersonagemId, out var personagem);
                    return new AdminPersonagemPopular
                    {
                        PersonagemId = p.PersonagemId,
                        Nome = p.Nome,
                        ImagemUrl = personagem?.ImagemUrl,
                        RaridadeNome = personagem?.Raridade?.Nome ?? "-",
                        RaridadeCor = personagem?.Raridade?.Cor,
                        Contas = p.TotalObtencoes,
                        Copias = copias.TryGetValue(p.PersonagemId, out var c) ? c : 0
                    };
                })
                .ToList();

            // ----- Invocações por banner (período) -----
            var porBanner = await _contexto.Invocacoes.AsNoTracking()
                .Where(i => i.Data >= desde)
                .GroupBy(i => i.BannerId)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToListAsync();
            var nomesBanner = await _contexto.Banners.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nome);

            r.InvocacoesPorBanner = porBanner
                .OrderByDescending(b => b.Total)
                .Select(b => new AdminContagem { Nome = nomesBanner.GetValueOrDefault(b.Key, "#" + b.Key), Total = b.Total })
                .ToList();

            // ----- Economia (saldo atual + movimentos no período) -----
            var tipos = await _contexto.TiposMoeda.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
            var saldos = await _contexto.Carteiras.AsNoTracking()
                .GroupBy(c => c.TipoMoedaId)
                .Select(g => new { g.Key, Total = g.Sum(c => c.Saldo) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);
            var movimentos = await _contexto.TransacoesMoeda.AsNoTracking()
                .Where(t => t.DataCriacao >= desde)
                .GroupBy(t => new { t.TipoMoedaId, t.TipoTransacao })
                .Select(g => new { g.Key.TipoMoedaId, g.Key.TipoTransacao, Total = g.Sum(t => t.Montante) })
                .ToListAsync();

            r.Economia = tipos.Select(t => new AdminEconomiaMoeda
            {
                Moeda = t.Nome,
                EmCirculacao = saldos.GetValueOrDefault(t.Id, 0m),
                Ganho = movimentos.Where(m => m.TipoMoedaId == t.Id && m.TipoTransacao == TransacaoMoeda.TipoGanho).Sum(m => m.Total),
                Gasto = movimentos.Where(m => m.TipoMoedaId == t.Id && m.TipoTransacao == TransacaoMoeda.TipoGasto).Sum(m => m.Total)
            }).ToList();

            // ----- Níveis de amizade (quantas personagens obtidas estão em cada nível) -----
            var niveis = await _contexto.NiveisAmizade.AsNoTracking().OrderBy(n => n.Ordem).ToListAsync();
            var porNivel = await _contexto.Colecoes.AsNoTracking()
                .GroupBy(c => c.NivelAmizadeId)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            r.NiveisAmizade = niveis
                .Select(n => new AdminContagem { Nome = n.Ordem + ". " + n.Nome, Total = porNivel.GetValueOrDefault(n.Id, 0) })
                .ToList();

            // ----- Ações de administração no período, por categoria -----
            var acoes = await _contexto.LogsAdministrador.AsNoTracking()
                .Where(l => l.DataCriacao >= desde)
                .Select(l => l.Acao)
                .ToListAsync();

            r.AcoesPorCategoria = acoes
                .GroupBy(a => a.Contains(':') ? a.Substring(0, a.IndexOf(':')) : a)
                .Select(g => new AdminContagem { Nome = g.Key, Total = g.Count() })
                .OrderByDescending(c => c.Total)
                .ToList();

            return r;
        }

        // ============================================================
        //  EXPORTAÇÃO (PDF / XML)
        // ============================================================

        // GET: api/admin/exportar?adminId=1&conjunto=utilizadores&formato=pdf
        [HttpGet("exportar")]
        public async Task<IActionResult> Exportar([FromQuery] int adminId, [FromQuery] string conjunto, [FromQuery] string formato = "pdf", [FromQuery] int dias = 30)
        {
            try
            {
                var admin = await ObterAdminAsync(adminId);
                if (admin == null)
                {
                    return ApenasAdmins("exportar dados");
                }

                conjunto = (conjunto ?? string.Empty).ToLowerInvariant();
                formato = (formato ?? "pdf").ToLowerInvariant();

                if (!Conjuntos.Contains(conjunto))
                {
                    return BadRequest("Conjunto desconhecido — use: " + string.Join(", ", Conjuntos) + ".");
                }

                if (formato != "pdf" && formato != "xml")
                {
                    return BadRequest("Formato desconhecido — use pdf ou xml.");
                }

                dias = Math.Clamp(dias, 7, 365);

                var relatorio = conjunto switch
                {
                    "estatisticas" => await RelatorioEstatisticasAsync(dias),
                    "utilizadores" => await RelatorioUtilizadoresAsync(),
                    "personagens" => await RelatorioPersonagensAsync(),
                    "banners" => await RelatorioBannersAsync(),
                    "acoes" => await RelatorioAcoesAsync(),
                    _ => await RelatorioTransacoesAsync(dias)
                };

                relatorio.Conjunto = conjunto;
                relatorio.GeradoPor = admin.NomeUtilizador;
                relatorio.GeradoEm = DateTime.UtcNow;

                byte[] conteudo = formato == "pdf"
                    ? ServicoExportacao.GerarPdf(relatorio)
                    : ServicoExportacao.GerarXml(relatorio);

                int linhas = relatorio.Seccoes.Sum(s => s.Tabela?.Linhas.Count ?? 0);
                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoExportacao + ": " + conjunto + " (" + formato.ToUpperInvariant() + ")",
                    null, null,
                    relatorio.Titulo + " — " + linhas + " linhas, " + conteudo.Length.ToString("N0", Pt) + " bytes.");
                await _contexto.SaveChangesAsync();

                string nomeFicheiro = "wishbound-" + conjunto + "-" + DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + "." + formato;
                return File(conteudo, formato == "pdf" ? "application/pdf" : "application/xml", nomeFicheiro);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao exportar: " + ex.Message);
            }
        }

        // ----- Relatórios -----

        private async Task<RelatorioExportacao> RelatorioEstatisticasAsync(int dias)
        {
            var e = await CalcularEstatisticasAsync(dias);
            var rel = new RelatorioExportacao { Titulo = "Estatísticas da plataforma (últimos " + dias + " dias)" };

            rel.Seccoes.Add(new SeccaoRelatorio
            {
                Titulo = "Resumo geral",
                ElementoXml = "Resumo",
                Valores =
                {
                    ("Contas ativas (vw_EstatisticasGerais)", N(e.UtilizadoresAtivos)),
                    ("Invocações desde sempre", N(e.TotalInvocacoes)),
                    ("Personagens obtidas (cópias)", N(e.TotalPersonagensObtidas)),
                    ("Contas no total", N(e.ContasTotal)),
                    ("Contas novas no período", N(e.NovasContas)),
                    ("Contas com login no período", N(e.ContasComLogin)),
                    ("Invocações no período", N(e.InvocacoesPeriodo)),
                    ("Invocações por garantia (pity)", N(e.InvocacoesComPity))
                }
            });

            rel.Seccoes.Add(new SeccaoRelatorio
            {
                Titulo = "Invocações por dia",
                ElementoXml = "InvocacoesPorDia",
                Barras = e.InvocacoesPorDia.Select(d => new BarraRelatorio
                {
                    Etiqueta = d.Data.ToString("ddd dd/MM", Pt),
                    Valor = d.Total,
                    TextoValor = N(d.Total)
                }).ToList()
            });

            rel.Seccoes.Add(new SeccaoRelatorio
            {
                Titulo = "Distribuição por raridade (vw_DistribuicaoRaridades)",
                ElementoXml = "DistribuicaoRaridades",
                Barras = e.DistribuicaoRaridades.Select(d => new BarraRelatorio
                {
                    Etiqueta = d.Raridade,
                    Valor = d.Obtidas,
                    TextoValor = N(d.Obtidas) + " · " + P(d.Percentagem) + " (esperado " + P(d.PercentagemEsperada) + ")",
                    CorHex = d.Cor
                }).ToList()
            });

            rel.Seccoes.Add(new SeccaoRelatorio
            {
                Titulo = "Personagens mais populares (vw_PersonagensMaisPopulares)",
                ElementoXml = "PersonagensPopulares",
                Tabela = new TabelaRelatorio
                {
                    ElementoLinha = "Personagem",
                    Colunas =
                    {
                        new ColunaRelatorio("#", "Posicao", true),
                        new ColunaRelatorio("Personagem", "Nome"),
                        new ColunaRelatorio("Raridade", "Raridade"),
                        new ColunaRelatorio("Contas que a têm", "Contas", true),
                        new ColunaRelatorio("Cópias", "Copias", true)
                    },
                    Linhas = e.PersonagensPopulares.Select((p, i) => new[] { (i + 1).ToString(), p.Nome, p.RaridadeNome, N(p.Contas), N(p.Copias) }).ToList()
                }
            });

            rel.Seccoes.Add(new SeccaoRelatorio
            {
                Titulo = "Invocações por banner (período)",
                ElementoXml = "InvocacoesPorBanner",
                Barras = e.InvocacoesPorBanner.Select(b => new BarraRelatorio { Etiqueta = b.Nome, Valor = b.Total, TextoValor = N(b.Total) }).ToList()
            });

            rel.Seccoes.Add(new SeccaoRelatorio
            {
                Titulo = "Economia",
                ElementoXml = "Economia",
                Tabela = new TabelaRelatorio
                {
                    ElementoLinha = "Moeda",
                    Colunas =
                    {
                        new ColunaRelatorio("Moeda", "Nome"),
                        new ColunaRelatorio("Em circulação", "EmCirculacao", true),
                        new ColunaRelatorio("Ganho no período", "Ganho", true),
                        new ColunaRelatorio("Gasto no período", "Gasto", true)
                    },
                    Linhas = e.Economia.Select(m => new[] { m.Moeda, D(m.EmCirculacao), D(m.Ganho), D(m.Gasto) }).ToList()
                }
            });

            rel.Seccoes.Add(new SeccaoRelatorio
            {
                Titulo = "Níveis de amizade (personagens obtidas por nível)",
                ElementoXml = "NiveisAmizade",
                Barras = e.NiveisAmizade.Select(n => new BarraRelatorio { Etiqueta = n.Nome, Valor = n.Total, TextoValor = N(n.Total) }).ToList()
            });

            if (e.AcoesPorCategoria.Count > 0)
            {
                rel.Seccoes.Add(new SeccaoRelatorio
                {
                    Titulo = "Ações de administração (período)",
                    ElementoXml = "AcoesAdministracao",
                    Barras = e.AcoesPorCategoria.Select(a => new BarraRelatorio { Etiqueta = a.Nome, Valor = a.Total, TextoValor = N(a.Total) }).ToList()
                });
            }

            return rel;
        }

        private async Task<RelatorioExportacao> RelatorioUtilizadoresAsync()
        {
            var contas = await _contexto.Utilizadores.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
            var carteiras = await _contexto.Carteiras.AsNoTracking().ToListAsync();
            var colecoes = await _contexto.Colecoes.AsNoTracking()
                .GroupBy(c => c.UtilizadorId)
                .Select(g => new { g.Key, Distintas = g.Count(), Copias = g.Sum(c => c.Quantidade) })
                .ToDictionaryAsync(x => x.Key);

            string Saldo(int id, int tipo) => D(carteiras.FirstOrDefault(c => c.UtilizadorId == id && c.TipoMoedaId == tipo)?.Saldo ?? 0m);

            return new RelatorioExportacao
            {
                Titulo = "Contas de utilizador (" + contas.Count + ")",
                Horizontal = true,
                Seccoes =
                {
                    new SeccaoRelatorio
                    {
                        Titulo = "Contas",
                        ElementoXml = "Utilizadores",
                        Tabela = new TabelaRelatorio
                        {
                            ElementoLinha = "Utilizador",
                            Colunas =
                            {
                                new ColunaRelatorio("Id", "Id", true),
                                new ColunaRelatorio("Utilizador", "Nome"),
                                new ColunaRelatorio("Email", "Email"),
                                new ColunaRelatorio("Estado", "Estado"),
                                new ColunaRelatorio("Admin", "Admin"),
                                new ColunaRelatorio("Criada", "DataCriacao"),
                                new ColunaRelatorio("Último login", "UltimoLogin"),
                                new ColunaRelatorio("Moedas", "Moedas", true),
                                new ColunaRelatorio("Bilhetes", "Bilhetes", true),
                                new ColunaRelatorio("Personagens", "Personagens", true),
                                new ColunaRelatorio("Cópias", "Copias", true)
                            },
                            Linhas = contas.Select(u =>
                            {
                                colecoes.TryGetValue(u.Id, out var c);
                                return new[]
                                {
                                    u.Id.ToString(), u.NomeUtilizador, u.Email,
                                    u.IsAtivo ? (u.EmailValidado ? "Ativa" : "Por validar") : "Desativada",
                                    u.IsAdmin ? "Sim" : "Não",
                                    Data(u.DataCriacao), u.UltimoLogin.HasValue ? Data(u.UltimoLogin.Value) : "-",
                                    Saldo(u.Id, TiposMoedaIds.Moedas), Saldo(u.Id, TiposMoedaIds.Bilhetes),
                                    N(c?.Distintas ?? 0), N(c?.Copias ?? 0)
                                };
                            }).ToList()
                        }
                    }
                }
            };
        }

        private async Task<RelatorioExportacao> RelatorioPersonagensAsync()
        {
            var personagens = await _contexto.Personagens.AsNoTracking().Include(p => p.Raridade)
                .OrderByDescending(p => p.Raridade!.Ordem).ThenBy(p => p.Nome).ToListAsync();
            var bannersDe = await _contexto.BannerPersonagens.AsNoTracking()
                .Join(_contexto.Banners, bp => bp.BannerId, b => b.Id, (bp, b) => new { bp.PersonagemId, b.Nome, bp.RateUp })
                .ToListAsync();
            var colecoes = await _contexto.Colecoes.AsNoTracking()
                .GroupBy(c => c.PersonagemId)
                .Select(g => new { g.Key, Contas = g.Count(), Copias = g.Sum(c => c.Quantidade) })
                .ToDictionaryAsync(x => x.Key);
            var invocacoes = await _contexto.Invocacoes.AsNoTracking()
                .GroupBy(i => i.PersonagemId)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            return new RelatorioExportacao
            {
                Titulo = "Personagens (" + personagens.Count + ")",
                Horizontal = true,
                Seccoes =
                {
                    new SeccaoRelatorio
                    {
                        Titulo = "Personagens",
                        ElementoXml = "Personagens",
                        Tabela = new TabelaRelatorio
                        {
                            ElementoLinha = "Personagem",
                            Colunas =
                            {
                                new ColunaRelatorio("Id", "Id", true),
                                new ColunaRelatorio("Nome", "Nome"),
                                new ColunaRelatorio("Raridade", "Raridade"),
                                new ColunaRelatorio("Ativa", "Ativa"),
                                new ColunaRelatorio("Banners", "Banners"),
                                new ColunaRelatorio("Invocações", "Invocacoes", true),
                                new ColunaRelatorio("Contas", "Contas", true),
                                new ColunaRelatorio("Cópias", "Copias", true),
                                new ColunaRelatorio("Criada", "DataCriacao")
                            },
                            Linhas = personagens.Select(p =>
                            {
                                colecoes.TryGetValue(p.Id, out var c);
                                string banners = string.Join(", ", bannersDe.Where(b => b.PersonagemId == p.Id).Select(b => b.Nome + (b.RateUp ? " (rate-up)" : "")));
                                return new[]
                                {
                                    p.Id.ToString(), p.Nome, p.Raridade?.Nome ?? "-", p.IsAtivo ? "Sim" : "Não",
                                    banners.Length == 0 ? "-" : banners,
                                    N(invocacoes.GetValueOrDefault(p.Id, 0)), N(c?.Contas ?? 0), N(c?.Copias ?? 0), Data(p.DataCriacao)
                                };
                            }).ToList()
                        }
                    }
                }
            };
        }

        private async Task<RelatorioExportacao> RelatorioBannersAsync()
        {
            var agora = DateTime.UtcNow;
            var banners = await _contexto.Banners.AsNoTracking().OrderBy(b => b.Id).ToListAsync();
            var pool = await _contexto.BannerPersonagens.AsNoTracking()
                .GroupBy(bp => bp.BannerId)
                .Select(g => new { g.Key, Total = g.Count(), RateUp = g.Count(x => x.RateUp) })
                .ToDictionaryAsync(x => x.Key);
            var invocacoes = await _contexto.Invocacoes.AsNoTracking()
                .GroupBy(i => i.BannerId)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Total);
            var participantes = await _contexto.ParticipacoesEventos.AsNoTracking()
                .GroupBy(p => p.BannerId)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            return new RelatorioExportacao
            {
                Titulo = "Banners e eventos (" + banners.Count + ")",
                Horizontal = true,
                Seccoes =
                {
                    new SeccaoRelatorio
                    {
                        Titulo = "Banners",
                        ElementoXml = "Banners",
                        Tabela = new TabelaRelatorio
                        {
                            ElementoLinha = "Banner",
                            Colunas =
                            {
                                new ColunaRelatorio("Id", "Id", true),
                                new ColunaRelatorio("Nome", "Nome"),
                                new ColunaRelatorio("Tipo", "Tipo"),
                                new ColunaRelatorio("Estado", "Estado"),
                                new ColunaRelatorio("Início", "Inicio"),
                                new ColunaRelatorio("Fim", "Fim"),
                                new ColunaRelatorio("Personagens", "Personagens", true),
                                new ColunaRelatorio("Rate-up", "RateUp", true),
                                new ColunaRelatorio("Invocações", "Invocacoes", true),
                                new ColunaRelatorio("Participantes", "Participantes", true)
                            },
                            Linhas = banners.Select(b =>
                            {
                                pool.TryGetValue(b.Id, out var p);
                                return new[]
                                {
                                    b.Id.ToString(), b.Nome, b.TipoBanner, EstadoBanner(b, agora),
                                    Data(b.DataInicio), b.DataFim.Year >= 9000 ? "-" : Data(b.DataFim),
                                    N(p?.Total ?? 0), N(p?.RateUp ?? 0),
                                    N(invocacoes.GetValueOrDefault(b.Id, 0)), N(participantes.GetValueOrDefault(b.Id, 0))
                                };
                            }).ToList()
                        }
                    }
                }
            };
        }

        private async Task<RelatorioExportacao> RelatorioAcoesAsync()
        {
            var acoes = await ObterAcoesRecentesAsync(500);

            return new RelatorioExportacao
            {
                Titulo = "Registo de ações de administração (últimas " + acoes.Count + ")",
                Horizontal = true,
                Seccoes =
                {
                    new SeccaoRelatorio
                    {
                        Titulo = "Ações",
                        ElementoXml = "Acoes",
                        Tabela = new TabelaRelatorio
                        {
                            ElementoLinha = "Acao",
                            Colunas =
                            {
                                new ColunaRelatorio("Data", "Data"),
                                new ColunaRelatorio("Administrador", "Administrador"),
                                new ColunaRelatorio("Conta", "Conta"),
                                new ColunaRelatorio("Ação", "Descricao"),
                                new ColunaRelatorio("Detalhes", "Detalhes")
                            },
                            Linhas = acoes.Select(a => new[]
                            {
                                Data(a.Data), a.AdminNome, a.UtilizadorAlvoNome ?? "-", a.Acao, a.Detalhes ?? string.Empty
                            }).ToList()
                        }
                    }
                }
            };
        }

        private async Task<RelatorioExportacao> RelatorioTransacoesAsync(int dias)
        {
            var desde = DateTime.UtcNow.AddDays(-dias);
            var nomesMoeda = await _contexto.TiposMoeda.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Nome);

            var transacoes = await _contexto.TransacoesMoeda.AsNoTracking()
                .Where(t => t.DataCriacao >= desde)
                .OrderByDescending(t => t.DataCriacao).ThenByDescending(t => t.Id)
                .Take(2000)
                .Join(_contexto.Utilizadores, t => t.UtilizadorId, u => u.Id, (t, u) => new { t, u.NomeUtilizador })
                .ToListAsync();

            var linhas = transacoes
                .OrderByDescending(x => x.t.DataCriacao).ThenByDescending(x => x.t.Id)
                .Select(x => new[]
                {
                    Data(x.t.DataCriacao), x.NomeUtilizador, nomesMoeda.GetValueOrDefault(x.t.TipoMoedaId, "?"),
                    x.t.TipoTransacao, (x.t.TipoTransacao == TransacaoMoeda.TipoGasto ? "-" : "+") + D(x.t.Montante), x.t.Origem
                }).ToList();

            return new RelatorioExportacao
            {
                Titulo = "Transações de moeda (últimos " + dias + " dias, " + linhas.Count + ")",
                Horizontal = true,
                Seccoes =
                {
                    new SeccaoRelatorio
                    {
                        Titulo = "Transações",
                        ElementoXml = "Transacoes",
                        Tabela = new TabelaRelatorio
                        {
                            ElementoLinha = "Transacao",
                            Colunas =
                            {
                                new ColunaRelatorio("Data", "Data"),
                                new ColunaRelatorio("Conta", "Conta"),
                                new ColunaRelatorio("Moeda", "Moeda"),
                                new ColunaRelatorio("Tipo", "Tipo"),
                                new ColunaRelatorio("Montante", "Montante", true),
                                new ColunaRelatorio("Origem", "Origem")
                            },
                            Linhas = linhas
                        }
                    }
                }
            };
        }

        // ----- Formatação (pt-PT) -----

        private static string N(int valor) => valor.ToString("N0", Pt);
        private static string D(decimal valor) => valor.ToString("#,##0.##", Pt);
        private static string P(decimal valor) => valor.ToString("0.##", Pt) + "%";
        private static string Data(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm", Pt);
    }
}
