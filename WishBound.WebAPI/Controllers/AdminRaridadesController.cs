using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;
using WishBound.WebAPI.Services;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// CRUD DAS RARIDADES (api/admin/raridades).
    ///
    ///   GET    api/admin/raridades?adminId=        - lista + personagens/invocações de cada
    ///   POST   api/admin/raridades                 - criar
    ///   PUT    api/admin/raridades/{id}            - editar nome, cor, probabilidade, escalão
    ///   DELETE api/admin/raridades/{id}?adminId=   - apagar (só sem personagens nem histórico)
    ///
    /// REGRAS
    ///   - Probabilidade é uma FRAÇÃO (0.55 = 55%) guardada em decimal(6,4);
    ///     no sorteio funciona como PESO (InvocacoesController.SortearPorPeso
    ///     divide pela soma), por isso a soma não tem de dar exatamente 1 —
    ///     mas a lista devolve a soma e a percentagem efetiva de cada uma,
    ///     e o site avisa quando não dá 100%.
    ///   - Ordem é o ESCALÃO (1 Comum … 5 Mítico): é o que o pity (Épica
    ///     aos 10, Lendária/Mítica aos 90), o 50/50 e o nível máximo de
    ///     amizade (escalão + 2) leem. Fica limitado a 1..5; duas raridades
    ///     podem partilhar o mesmo escalão (seguem as mesmas regras).
    ///   - Nome único (UNIQUE na BD) e cor em #RRGGBB.
    ///   - Apagar só se nenhuma personagem nem invocação a usar (as FKs de
    ///     Personagens, HistoricoInvocacoes e PityUtilizador impediriam).
    /// Cada escrita fica em LogsAdministrador (categoria "Raridade").
    /// </summary>
    [Route("api/admin/raridades")]
    [ApiController]
    public class AdminRaridadesController : AdminBaseController
    {
        private const int EscalaoMinimo = 1;
        private const int EscalaoMaximo = 5;

        private static readonly Regex CorHex = new Regex("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

        public AdminRaridadesController(WishBoundContext contexto) : base(contexto)
        {
        }

        // GET: api/admin/raridades?adminId=1
        [HttpGet]
        public async Task<ActionResult<AdminListaRaridades>> Obter([FromQuery] int adminId)
        {
            try
            {
                if (await ObterAdminAsync(adminId) == null)
                {
                    return ApenasAdmins("gerir as raridades");
                }

                var raridades = await _contexto.Raridades.AsNoTracking().OrderBy(r => r.Ordem).ThenBy(r => r.Id).ToListAsync();

                var personagens = await _contexto.Personagens.AsNoTracking()
                    .GroupBy(p => p.RaridadeId)
                    .Select(g => new { RaridadeId = g.Key, Total = g.Count() })
                    .ToDictionaryAsync(x => x.RaridadeId, x => x.Total);

                var invocacoes = await _contexto.Invocacoes.AsNoTracking()
                    .GroupBy(i => i.RaridadeId)
                    .Select(g => new { RaridadeId = g.Key, Total = g.Count() })
                    .ToDictionaryAsync(x => x.RaridadeId, x => x.Total);

                decimal soma = raridades.Sum(r => r.Probabilidade);

                var resposta = new AdminListaRaridades { SomaProbabilidades = soma };

                foreach (var r in raridades)
                {
                    int nPersonagens = personagens.TryGetValue(r.Id, out var np) ? np : 0;
                    int nInvocacoes = invocacoes.TryGetValue(r.Id, out var ni) ? ni : 0;

                    resposta.Itens.Add(new AdminRaridadeResposta
                    {
                        Id = r.Id,
                        Nome = r.Nome,
                        Cor = r.Cor,
                        Probabilidade = r.Probabilidade,
                        PercentagemEfetiva = soma > 0 ? Math.Round(100m * r.Probabilidade / soma, 2) : 0m,
                        Ordem = r.Ordem,
                        NivelAmizadeMaximo = ServicoAmizade.NivelMaximo(r.Ordem),
                        Personagens = nPersonagens,
                        Invocacoes = nInvocacoes,
                        PodeApagar = nPersonagens == 0 && nInvocacoes == 0
                    });
                }

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter as raridades: " + ex.Message);
            }
        }

        // POST: api/admin/raridades
        [HttpPost]
        public async Task<ActionResult<AdminAcaoResultado>> Criar([FromBody] AdminRaridadePedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir as raridades");
                }

                string? erro = await ValidarAsync(pedido, idAtual: null);
                if (erro != null)
                {
                    return BadRequest(erro);
                }

                var raridade = new Raridade
                {
                    Nome = pedido.Nome.Trim(),
                    Cor = pedido.Cor!.Trim().ToLowerInvariant(),
                    Probabilidade = Math.Round(pedido.Probabilidade, 4),
                    Ordem = pedido.Ordem
                };

                // Raridade + registo na mesma transação (o registo precisa do Id novo)
                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                _contexto.Raridades.Add(raridade);
                await _contexto.SaveChangesAsync();

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoRaridade + ": criada " + raridade.Nome,
                    "Raridades", raridade.Id,
                    ComMotivo("Probabilidade " + Percentagem(raridade.Probabilidade) + ", escalão " + raridade.Ordem + ", cor " + raridade.Cor + ".", pedido.Motivo));
                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Raridade \"" + raridade.Nome + "\" criada." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao criar a raridade: " + ex.Message);
            }
        }

        // PUT: api/admin/raridades/5
        [HttpPut("{id:int}")]
        public async Task<ActionResult<AdminAcaoResultado>> Editar(int id, [FromBody] AdminRaridadePedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir as raridades");
                }

                var raridade = await _contexto.Raridades.FirstOrDefaultAsync(r => r.Id == id);
                if (raridade == null)
                {
                    return NotFound("Raridade não encontrada.");
                }

                string? erro = await ValidarAsync(pedido, idAtual: id);
                if (erro != null)
                {
                    return BadRequest(erro);
                }

                var mudancas = new List<string>();
                string nome = pedido.Nome.Trim();
                string cor = pedido.Cor!.Trim().ToLowerInvariant();
                decimal probabilidade = Math.Round(pedido.Probabilidade, 4);

                if (nome != raridade.Nome) mudancas.Add("nome " + raridade.Nome + " → " + nome);
                if (!string.Equals(cor, raridade.Cor, StringComparison.OrdinalIgnoreCase)) mudancas.Add("cor " + raridade.Cor + " → " + cor);
                if (probabilidade != raridade.Probabilidade) mudancas.Add("probabilidade " + Percentagem(raridade.Probabilidade) + " → " + Percentagem(probabilidade));
                if (pedido.Ordem != raridade.Ordem) mudancas.Add("escalão " + raridade.Ordem + " → " + pedido.Ordem);

                if (mudancas.Count == 0)
                {
                    return Ok(new AdminAcaoResultado { Mensagem = "Nada foi alterado." });
                }

                raridade.Nome = nome;
                raridade.Cor = cor;
                raridade.Probabilidade = probabilidade;
                raridade.Ordem = pedido.Ordem;

                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoRaridade + ": editada " + nome,
                    "Raridades", raridade.Id,
                    ComMotivo(Capitalizar(string.Join(", ", mudancas)) + ".", pedido.Motivo));

                await _contexto.SaveChangesAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Raridade \"" + nome + "\" atualizada." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao atualizar a raridade: " + ex.Message);
            }
        }

        // DELETE: api/admin/raridades/5?adminId=1&motivo=...
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<AdminAcaoResultado>> Apagar(int id, [FromQuery] int adminId, [FromQuery] string? motivo)
        {
            try
            {
                var admin = await ObterAdminAsync(adminId);
                if (admin == null)
                {
                    return ApenasAdmins("gerir as raridades");
                }

                var raridade = await _contexto.Raridades.FirstOrDefaultAsync(r => r.Id == id);
                if (raridade == null)
                {
                    return NotFound("Raridade não encontrada.");
                }

                if (await _contexto.Personagens.AnyAsync(p => p.RaridadeId == id))
                {
                    return BadRequest("Há personagens com a raridade \"" + raridade.Nome + "\" — mude-as de raridade antes de a apagar.");
                }

                if (await _contexto.Invocacoes.AnyAsync(i => i.RaridadeId == id) ||
                    await _contexto.Pity.AnyAsync(p => p.UltimaRaridadeGarantida == id))
                {
                    return BadRequest("A raridade \"" + raridade.Nome + "\" já aparece no histórico de invocações — não pode ser apagada.");
                }

                if (await _contexto.Raridades.CountAsync() <= 1)
                {
                    return BadRequest("Tem de existir pelo menos uma raridade.");
                }

                _contexto.Raridades.Remove(raridade);
                RegistarAcao(admin.Id, null,
                    LogAdministrador.AcaoRaridade + ": apagada " + raridade.Nome,
                    "Raridades", raridade.Id,
                    ComMotivo("Probabilidade era " + Percentagem(raridade.Probabilidade) + ".", motivo));

                await _contexto.SaveChangesAsync();

                return Ok(new AdminAcaoResultado { Mensagem = "Raridade \"" + raridade.Nome + "\" apagada." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao apagar a raridade: " + ex.Message);
            }
        }

        // ------------------------------------------------------------

        private async Task<string?> ValidarAsync(AdminRaridadePedido pedido, int? idAtual)
        {
            string nome = (pedido.Nome ?? string.Empty).Trim();

            if (nome.Length < 2 || nome.Length > 30)
            {
                return "O nome deve ter entre 2 e 30 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(pedido.Cor) || !CorHex.IsMatch(pedido.Cor.Trim()))
            {
                return "A cor deve estar no formato #RRGGBB (ex.: #f3c04f).";
            }

            if (pedido.Probabilidade < 0.0001m || pedido.Probabilidade > 1m)
            {
                return "A probabilidade deve estar entre 0,01% e 100%.";
            }

            if (pedido.Ordem < EscalaoMinimo || pedido.Ordem > EscalaoMaximo)
            {
                return "O escalão deve estar entre 1 (Comum) e 5 (Mítico).";
            }

            bool nomeRepetido = await _contexto.Raridades
                .AnyAsync(r => r.Nome == nome && (!idAtual.HasValue || r.Id != idAtual.Value));

            if (nomeRepetido)
            {
                return "Já existe uma raridade com o nome \"" + nome + "\".";
            }

            return null;
        }

        private static string Percentagem(decimal fracao) =>
            (fracao * 100m).ToString("0.##", CultureInfo.GetCultureInfo("pt-PT")) + "%";

        private static string Capitalizar(string texto) =>
            string.IsNullOrEmpty(texto) ? texto : char.ToUpperInvariant(texto[0]) + texto.Substring(1);
    }
}
