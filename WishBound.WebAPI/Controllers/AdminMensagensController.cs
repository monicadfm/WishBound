using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;
using WishBound.WebAPI.Services;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// ADMINISTRAÇÃO DAS MENSAGENS DE PERSONAGEM (api/admin/mensagens).
    /// CRUD dos conjuntos de mensagens de cada personagem:
    ///
    ///   GET    api/admin/mensagens?adminId=&personagemId=   - resumo por personagem +
    ///                                                         mensagens (todas ou de uma)
    ///   GET    api/admin/mensagens/{id}?adminId=            - uma mensagem
    ///   POST   api/admin/mensagens                          - criar
    ///   PUT    api/admin/mensagens/{id}                     - editar
    ///   DELETE api/admin/mensagens/{id}?adminId=            - apagar
    ///
    /// REGRAS: o tipo tem de ser Saudacao / Aleatoria / Diaria (CHECK da
    /// tabela); o nível é a ordem 1..7; as mensagens DIÁRIAS só podem
    /// existir a partir do nível 4 (são a recompensa desse nível). Uma
    /// mensagem num nível que a raridade da personagem não alcança é aceite
    /// mas assinalada como "não alcançável" (nunca será vista). Uma frase
    /// só pode pertencer a UMA personagem — as falas são todas diferentes
    /// entre personagens.
    ///
    /// Cada escrita fica no registo de ações (LogsAdministrador), como o
    /// resto da administração — categoria "Mensagem".
    /// </summary>
    [Route("api/admin/mensagens")]
    [ApiController]
    public class AdminMensagensController : ControllerBase
    {
        private readonly WishBoundContext _contexto;
        private readonly ServicoAmizade _amizade;

        public AdminMensagensController(WishBoundContext contexto, ServicoAmizade amizade)
        {
            _contexto = contexto;
            _amizade = amizade;
        }

        // ------------------------------------------------------------
        // GET: api/admin/mensagens?adminId=1&personagemId=3
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<AdminListaMensagens>> Obter([FromQuery] int adminId, [FromQuery] int? personagemId)
        {
            try
            {
                if (await ObterAdminAsync(adminId) == null)
                {
                    return StatusCode(403, "Apenas administradores podem gerir as mensagens.");
                }

                var niveis = await _amizade.ObterNiveisAsync();
                var ordemPorId = niveis.ToDictionary(n => n.Id, n => n.Ordem);
                var nomePorId = niveis.ToDictionary(n => n.Id, n => n.Nome);

                var personagens = await _contexto.Personagens.AsNoTracking()
                    .Include(p => p.Raridade)
                    .OrderBy(p => p.Raridade!.Ordem).ThenBy(p => p.Nome)
                    .ToListAsync();

                var contagens = await _contexto.MensagensPersonagem.AsNoTracking()
                    .GroupBy(m => new { m.PersonagemId, m.TipoMensagem })
                    .Select(g => new { g.Key.PersonagemId, g.Key.TipoMensagem, Total = g.Count() })
                    .ToListAsync();

                var resposta = new AdminListaMensagens
                {
                    NivelMinimoDiaria = ServicoMensagens.NivelMensagensDiarias,
                    Niveis = niveis.Select(n => new NivelAmizadeResposta
                    {
                        Id = n.Id,
                        Nome = n.Nome,
                        Ordem = n.Ordem,
                        PontosNecessarios = n.PontosNecessarios,
                        Recompensa = ServicoAmizade.DescricaoRecompensa(n.Ordem)
                    }).ToList()
                };

                foreach (var p in personagens)
                {
                    resposta.Personagens.Add(new AdminPersonagemMensagens
                    {
                        PersonagemId = p.Id,
                        Nome = p.Nome,
                        ImagemUrl = p.ImagemUrl,
                        RaridadeNome = p.Raridade?.Nome ?? "Desconhecida",
                        RaridadeCor = p.Raridade?.Cor,
                        NivelMaximoOrdem = ServicoAmizade.NivelMaximo(p.Raridade?.Ordem ?? 1),
                        Saudacoes = contagens.Where(c => c.PersonagemId == p.Id && c.TipoMensagem == MensagemPersonagem.TipoSaudacao).Sum(c => c.Total),
                        Aleatorias = contagens.Where(c => c.PersonagemId == p.Id && c.TipoMensagem == MensagemPersonagem.TipoAleatoria).Sum(c => c.Total),
                        Diarias = contagens.Where(c => c.PersonagemId == p.Id && c.TipoMensagem == MensagemPersonagem.TipoDiaria).Sum(c => c.Total)
                    });
                }

                IQueryable<MensagemPersonagem> consulta = _contexto.MensagensPersonagem.AsNoTracking()
                    .Include(m => m.Personagem)
                        .ThenInclude(p => p!.Raridade);

                if (personagemId.HasValue && personagemId.Value > 0)
                {
                    consulta = consulta.Where(m => m.PersonagemId == personagemId.Value);
                }

                var mensagens = await consulta.ToListAsync();

                resposta.Mensagens = mensagens
                    .Select(m => ParaResposta(m, ordemPorId, nomePorId))
                    .OrderBy(m => m.PersonagemNome)
                    .ThenBy(m => ServicoMensagens.OrdemTipo(m.Tipo))
                    .ThenBy(m => m.NivelOrdem)
                    .ThenBy(m => m.Id)
                    .ToList();

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter as mensagens: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/admin/mensagens/12?adminId=1
        // ------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminMensagemResposta>> ObterUma(int id, [FromQuery] int adminId)
        {
            try
            {
                if (await ObterAdminAsync(adminId) == null)
                {
                    return StatusCode(403, "Apenas administradores podem gerir as mensagens.");
                }

                var mensagem = await _contexto.MensagensPersonagem.AsNoTracking()
                    .Include(m => m.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mensagem == null)
                {
                    return NotFound("Mensagem não encontrada.");
                }

                var niveis = await _amizade.ObterNiveisAsync();
                return Ok(ParaResposta(mensagem,
                    niveis.ToDictionary(n => n.Id, n => n.Ordem),
                    niveis.ToDictionary(n => n.Id, n => n.Nome)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a mensagem: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/admin/mensagens   (criar)
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<AdminAcaoResultado>> Criar([FromBody] AdminMensagemPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem gerir as mensagens.");
                }

                var personagem = await _contexto.Personagens.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == pedido.PersonagemId);

                if (personagem == null)
                {
                    return BadRequest("Personagem não encontrada.");
                }

                var (nivel, erro) = await ValidarAsync(pedido);
                if (nivel == null)
                {
                    return BadRequest(erro);
                }

                var mensagem = new MensagemPersonagem
                {
                    PersonagemId = personagem.Id,
                    TipoMensagem = pedido.Tipo,
                    NivelAmizadeId = nivel.Id,
                    Conteudo = pedido.Conteudo.Trim()
                };

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                _contexto.MensagensPersonagem.Add(mensagem);
                await _contexto.SaveChangesAsync();

                RegistarAcao(admin.Id,
                    "Mensagem: criada (" + ServicoMensagens.NomeTipo(pedido.Tipo) + " de " + personagem.Nome + ")",
                    mensagem.Id,
                    ComMotivo("Nível " + nivel.Ordem + " (" + nivel.Nome + "): \"" + Truncar(mensagem.Conteudo, 200) + "\".", pedido.Motivo));
                await _contexto.SaveChangesAsync();

                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = ServicoMensagens.NomeTipo(pedido.Tipo) + " de " + personagem.Nome + " criada (nível " + nivel.Ordem + " — " + nivel.Nome + ").",
                    NovoValor = mensagem.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao criar a mensagem: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // PUT: api/admin/mensagens/12   (editar)
        // ------------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<ActionResult<AdminAcaoResultado>> Editar(int id, [FromBody] AdminMensagemPedido pedido)
        {
            try
            {
                var admin = await ObterAdminAsync(pedido.AdminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem gerir as mensagens.");
                }

                var mensagem = await _contexto.MensagensPersonagem
                    .Include(m => m.Personagem)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mensagem == null)
                {
                    return NotFound("Mensagem não encontrada.");
                }

                // A personagem de uma mensagem não muda (apaga-se e cria-se outra)
                pedido.PersonagemId = mensagem.PersonagemId;

                var (nivel, erro) = await ValidarAsync(pedido);
                if (nivel == null)
                {
                    return BadRequest(erro);
                }

                string antes = "\"" + Truncar(mensagem.Conteudo, 120) + "\" (" + ServicoMensagens.NomeTipo(mensagem.TipoMensagem) + ", nível Id " + mensagem.NivelAmizadeId + ")";

                mensagem.TipoMensagem = pedido.Tipo;
                mensagem.NivelAmizadeId = nivel.Id;
                mensagem.Conteudo = pedido.Conteudo.Trim();

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                RegistarAcao(admin.Id,
                    "Mensagem: editada (" + ServicoMensagens.NomeTipo(pedido.Tipo) + " de " + (mensagem.Personagem?.Nome ?? "?") + ")",
                    mensagem.Id,
                    ComMotivo("Antes: " + antes + ". Agora: nível " + nivel.Ordem + " \"" + Truncar(mensagem.Conteudo, 120) + "\".", pedido.Motivo));

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado
                {
                    Mensagem = "Mensagem de " + (mensagem.Personagem?.Nome ?? "?") + " atualizada.",
                    NovoValor = mensagem.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao editar a mensagem: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // DELETE: api/admin/mensagens/12?adminId=1&motivo=...
        // ------------------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<ActionResult<AdminAcaoResultado>> Apagar(int id, [FromQuery] int adminId, [FromQuery] string? motivo)
        {
            try
            {
                var admin = await ObterAdminAsync(adminId);
                if (admin == null)
                {
                    return StatusCode(403, "Apenas administradores podem gerir as mensagens.");
                }

                var mensagem = await _contexto.MensagensPersonagem
                    .Include(m => m.Personagem)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mensagem == null)
                {
                    return NotFound("Mensagem não encontrada.");
                }

                string nomePersonagem = mensagem.Personagem?.Nome ?? "?";
                string tipo = ServicoMensagens.NomeTipo(mensagem.TipoMensagem);

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                RegistarAcao(admin.Id,
                    "Mensagem: apagada (" + tipo + " de " + nomePersonagem + ")",
                    mensagem.Id,
                    ComMotivo("\"" + Truncar(mensagem.Conteudo, 200) + "\".", motivo));

                _contexto.MensagensPersonagem.Remove(mensagem);
                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new AdminAcaoResultado { Mensagem = tipo + " de " + nomePersonagem + " apagada." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao apagar a mensagem: " + ex.Message);
            }
        }

        // ============================================================
        //  Auxiliares
        // ============================================================

        /// <summary>Tipo, nível e conteúdo válidos → o nível; senão a mensagem de erro.</summary>
        private async Task<(NivelAmizade? Nivel, string? Erro)> ValidarAsync(AdminMensagemPedido pedido)
        {
            if (!MensagemPersonagem.Tipos.Contains(pedido.Tipo))
            {
                return (null, "O tipo tem de ser Saudacao, Aleatoria ou Diaria.");
            }

            if (string.IsNullOrWhiteSpace(pedido.Conteudo) || pedido.Conteudo.Trim().Length < 2)
            {
                return (null, "A mensagem não pode estar vazia.");
            }

            if (pedido.Conteudo.Trim().Length > 500)
            {
                return (null, "A mensagem não pode ter mais de 500 caracteres.");
            }

            int minimo = ServicoMensagens.NivelMinimo(pedido.Tipo);
            if (pedido.NivelOrdem < minimo)
            {
                return (null, pedido.Tipo == MensagemPersonagem.TipoDiaria
                    ? "As mensagens diárias só existem a partir do nível " + minimo + " (Confidente) — é a recompensa desse nível."
                    : "O nível tem de ser pelo menos " + minimo + ".");
            }

            // Cada personagem tem as suas falas: a mesma frase não pode
            // pertencer a duas personagens.
            string conteudo = pedido.Conteudo.Trim();
            var outra = await _contexto.MensagensPersonagem.AsNoTracking()
                .Where(m => m.PersonagemId != pedido.PersonagemId && m.Conteudo == conteudo)
                .Select(m => m.Personagem!.Nome)
                .FirstOrDefaultAsync();

            if (outra != null)
            {
                return (null, "Essa frase já pertence a " + outra + " — cada personagem tem as suas próprias falas.");
            }

            var nivel = await _contexto.NiveisAmizade.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Ordem == pedido.NivelOrdem);

            return nivel == null ? (null, "Nível de amizade inválido (1 a 7).") : (nivel, null);
        }

        private async Task<Utilizador?> ObterAdminAsync(int adminId)
        {
            if (adminId <= 0)
            {
                return null;
            }

            return await _contexto.Utilizadores.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == adminId && u.IsAdmin && u.IsAtivo);
        }

        /// <summary>Linha do registo de ações — gravada por quem chama, dentro da mesma transação.</summary>
        private void RegistarAcao(int adminId, string acao, int? registoId, string? detalhes)
        {
            _contexto.LogsAdministrador.Add(new LogAdministrador
            {
                AdminId = adminId,
                UtilizadorAlvoId = null,
                Acao = Truncar(acao, 100),
                TabelaAlvo = "MensagensPersonagem",
                RegistoAlvoId = registoId,
                Detalhes = detalhes == null ? null : Truncar(detalhes, 500),
                DataCriacao = DateTime.UtcNow
            });
        }

        private static AdminMensagemResposta ParaResposta(MensagemPersonagem m, Dictionary<int, int> ordemPorId, Dictionary<int, string> nomePorId)
        {
            int ordem = ordemPorId.TryGetValue(m.NivelAmizadeId, out var o) ? o : 0;
            int maximo = ServicoAmizade.NivelMaximo(m.Personagem?.Raridade?.Ordem ?? 1);

            return new AdminMensagemResposta
            {
                Id = m.Id,
                PersonagemId = m.PersonagemId,
                PersonagemNome = m.Personagem?.Nome ?? "?",
                Tipo = m.TipoMensagem,
                NivelOrdem = ordem,
                NivelNome = nomePorId.TryGetValue(m.NivelAmizadeId, out var n) ? n : "?",
                Conteudo = m.Conteudo,
                Alcancavel = ordem <= maximo
            };
        }

        private static string ComMotivo(string detalhes, string? motivo) =>
            string.IsNullOrWhiteSpace(motivo) ? detalhes : detalhes + " Motivo: " + motivo.Trim();

        private static string Truncar(string texto, int max) =>
            texto.Length <= max ? texto : texto.Substring(0, max - 1) + "…";
    }
}
