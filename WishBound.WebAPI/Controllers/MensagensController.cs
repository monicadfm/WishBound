using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;
using WishBound.WebAPI.Services;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// MENSAGENS DE PERSONAGEM (api/mensagens) — o lado do utilizador.
    ///
    ///   GET  api/mensagens/personagem?utilizadorId=&personagemId=
    ///        - o conjunto de mensagens de uma personagem da coleção com o
    ///          que já está desbloqueado (e o que falta), mais a saudação
    ///          de hoje — para a página de detalhes;
    ///   GET  api/mensagens/companheira?utilizadorId=
    ///        - a companheira escolhida para a página inicial, a saudação
    ///          dela e as personagens da coleção que podem ser escolhidas;
    ///   POST api/mensagens/companheira
    ///        - escolhe (ou tira) a companheira — só pode ser uma, e tem de
    ///          estar na coleção do utilizador.
    ///
    /// A reação a uma interação e a mensagem diária usam o mesmo
    /// ServicoMensagens a partir do AmizadeController / ServicoAmizade.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MensagensController : ControllerBase
    {
        private readonly WishBoundContext _contexto;
        private readonly ServicoMensagens _mensagens;
        private readonly ServicoAmizade _amizade;

        public MensagensController(WishBoundContext contexto, ServicoMensagens mensagens, ServicoAmizade amizade)
        {
            _contexto = contexto;
            _mensagens = mensagens;
            _amizade = amizade;
        }

        // ------------------------------------------------------------
        // GET: api/mensagens/personagem?utilizadorId=5&personagemId=3
        // ------------------------------------------------------------
        [HttpGet("personagem")]
        public async Task<ActionResult<MensagensPersonagemResposta>> ObterPersonagem(
            [FromQuery] int utilizadorId, [FromQuery] int personagemId)
        {
            try
            {
                if (utilizadorId <= 0 || personagemId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador e a personagem.");
                }

                var utilizador = await _contexto.Utilizadores.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == utilizadorId && u.IsAtivo);

                if (utilizador == null)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                var item = await _contexto.Colecoes.AsNoTracking()
                    .Include(c => c.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .FirstOrDefaultAsync(c => c.UtilizadorId == utilizadorId && c.PersonagemId == personagemId);

                if (item == null || item.Personagem == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                var niveis = await _amizade.ObterNiveisAsync();
                var estado = AmizadeController.ParaResposta(item, niveis);

                var conjunto = await _mensagens.ObterConjuntoAsync(personagemId, estado.NivelOrdem, estado.NivelMaximoOrdem);

                return Ok(new MensagensPersonagemResposta
                {
                    PersonagemId = personagemId,
                    Nome = item.Personagem.Nome,
                    NivelOrdem = estado.NivelOrdem,
                    NivelMaximoOrdem = estado.NivelMaximoOrdem,
                    Saudacao = await _mensagens.EscolherAsync(personagemId, MensagemPersonagem.TipoSaudacao, estado.NivelOrdem),
                    EhCompanheira = utilizador.PersonagemCompanheiraId == personagemId,
                    NivelMensagensDiarias = ServicoMensagens.NivelMensagensDiarias,
                    Total = conjunto.Count,
                    Desbloqueadas = conjunto.Count(m => m.Desbloqueada),
                    Mensagens = conjunto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter as mensagens da personagem: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/mensagens/companheira?utilizadorId=5
        // ------------------------------------------------------------
        [HttpGet("companheira")]
        public async Task<ActionResult<CompanheiraResposta>> ObterCompanheira([FromQuery] int utilizadorId)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                var utilizador = await _contexto.Utilizadores.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == utilizadorId && u.IsAtivo);

                if (utilizador == null)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                var niveis = await _amizade.ObterNiveisAsync();

                var colecao = await _contexto.Colecoes.AsNoTracking()
                    .Include(c => c.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .Where(c => c.UtilizadorId == utilizadorId && c.Personagem != null && c.Personagem.IsAtivo)
                    .ToListAsync();

                var resposta = new CompanheiraResposta();

                foreach (var item in colecao
                    .OrderByDescending(c => c.NivelAmizadeId)
                    .ThenByDescending(c => c.Personagem!.Raridade?.Ordem ?? 0)
                    .ThenBy(c => c.Personagem!.Nome))
                {
                    var estado = AmizadeController.ParaResposta(item, niveis);
                    var candidata = new CandidataCompanheira
                    {
                        PersonagemId = item.PersonagemId,
                        Nome = estado.Nome,
                        ImagemUrl = estado.ImagemUrl,
                        RaridadeNome = estado.RaridadeNome,
                        RaridadeCor = estado.RaridadeCor,
                        NivelAmizadeNome = estado.NivelAmizadeNome,
                        NivelOrdem = estado.NivelOrdem
                    };

                    resposta.Candidatas.Add(candidata);

                    if (utilizador.PersonagemCompanheiraId == item.PersonagemId)
                    {
                        resposta.Escolhida = candidata;
                    }
                }

                if (resposta.Escolhida != null)
                {
                    resposta.Saudacao = await _mensagens.EscolherAsync(
                        resposta.Escolhida.PersonagemId, MensagemPersonagem.TipoSaudacao, resposta.Escolhida.NivelOrdem)
                        ?? SaudacaoGenerica(resposta.Escolhida.Nome, resposta.Escolhida.NivelOrdem);
                }

                return Ok(resposta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a companheira: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/mensagens/companheira  { UtilizadorId, PersonagemId? }
        // ------------------------------------------------------------
        [HttpPost("companheira")]
        public async Task<IActionResult> EscolherCompanheira([FromBody] EscolherCompanheiraPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.FindAsync(pedido.UtilizadorId);
                if (utilizador == null || !utilizador.IsAtivo)
                {
                    return BadRequest("Utilizador inválido.");
                }

                if (!pedido.PersonagemId.HasValue)
                {
                    utilizador.PersonagemCompanheiraId = null;
                    await _contexto.SaveChangesAsync();
                    return Ok("Já não tens companheira na página inicial.");
                }

                // Só pode escolher personagens que tem na coleção
                var item = await _contexto.Colecoes.AsNoTracking()
                    .Include(c => c.Personagem)
                    .FirstOrDefaultAsync(c => c.UtilizadorId == utilizador.Id && c.PersonagemId == pedido.PersonagemId.Value);

                if (item == null || item.Personagem == null)
                {
                    return BadRequest("Só podes escolher uma personagem da tua coleção.");
                }

                if (utilizador.PersonagemCompanheiraId == item.PersonagemId)
                {
                    return Ok(item.Personagem.Nome + " já é a tua companheira.");
                }

                utilizador.PersonagemCompanheiraId = item.PersonagemId;
                await _contexto.SaveChangesAsync();

                return Ok(item.Personagem.Nome + " passa a receber-te na página inicial.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao escolher a companheira: " + ex.Message);
            }
        }

        /// <summary>Saudação de recurso quando a personagem não tem conjunto de saudações.</summary>
        public static string SaudacaoGenerica(string nome, int nivelOrdem) => nivelOrdem switch
        {
            1 => nome + " olha para ti com curiosidade.",
            2 => nome + " acena-te. Já te reconhece.",
            3 => nome + " sorri: \"Que bom ver-te outra vez!\"",
            4 => nome + ": \"Estava à tua espera!\"",
            5 => nome + ": \"Contigo é diferente.\"",
            6 => nome + " fica em silêncio ao teu lado. Não é preciso dizer nada.",
            _ => nome + " brilha só de te ver."
        };
    }
}
