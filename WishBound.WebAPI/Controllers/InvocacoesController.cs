using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Sistema de invocação (gacha):
    /// escolhe uma raridade de forma aleatória ponderada pelas probabilidades
    /// e devolve uma personagem dessa raridade. Guarda o resultado no histórico.
    ///
    /// Desde a versão com autenticação, cada invocação pertence a um utilizador
    /// real: o site envia o Id do utilizador autenticado e o histórico é
    /// consultado POR utilizador. O banner continua a ser o "Banner Permanente"
    /// (Id 1) criado pela migração — a escolha de banner chega com a
    /// funcionalidade de eventos/banners temporários.
    ///
    /// A personagem obtida entra também na COLEÇÃO do utilizador
    /// (ColecaoUtilizador): primeira vez cria a linha, repetida soma uma cópia.
    /// Antes de sortear é verificado se ainda há espaço no inventário — cada
    /// cópia ocupa um lugar — e, se não houver, a invocação é recusada sem
    /// gastar nada.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InvocacoesController : ControllerBase
    {
        // Id fixo criado pelo script Database/Migracao01.sql.
        // Será substituído pelo banner escolhido quando existirem banners de evento.
        private const int BannerPermanenteId = 1;

        // Primeiro nível de amizade ("Desconhecido") - usado ao criar a linha
        // da coleção; a progressão chega com o sistema de amizade.
        private const int NivelAmizadeInicialId = 1;

        private readonly WishBoundContext _contexto;

        public InvocacoesController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/invocacoes?utilizadorId=5  (histórico DO utilizador - SELECT com JOIN)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invocacao>>> GetInvocacoes([FromQuery] int utilizadorId)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                var historico = await _contexto.Invocacoes
                    .Where(i => i.UtilizadorId == utilizadorId)
                    .Include(i => i.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .OrderByDescending(i => i.Data)
                    .Take(50)
                    .ToListAsync();

                return Ok(historico);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o histórico de invocações: " + ex.Message);
            }
        }

        // POST: api/invocacoes  (realiza uma invocação - INSERT)
        // O corpo indica QUEM está a invocar: { "utilizadorId": 5 }
        [HttpPost]
        public async Task<ActionResult<InvocacaoResultado>> Invocar([FromBody] InvocacaoPedido pedido)
        {
            try
            {
                // O utilizador tem de existir e estar ativo — protege contra
                // pedidos com Ids inventados (o histórico tem FK para Utilizadores).
                var utilizador = await _contexto.Utilizadores.FindAsync(pedido.UtilizadorId);
                if (utilizador == null || !utilizador.IsAtivo)
                {
                    return BadRequest("Utilizador inválido para invocar.");
                }

                // ESPAÇO NA COLEÇÃO: cada cópia (incluindo as repetidas) ocupa
                // um lugar. Sem espaço, a invocação é recusada ANTES do sorteio,
                // para o utilizador não perder uma invocação.
                var inventario = await ObterOuCriarInventarioAsync(utilizador.Id);

                int ocupado = await _contexto.Colecoes
                    .Where(c => c.UtilizadorId == utilizador.Id)
                    .SumAsync(c => (int?)c.Quantidade) ?? 0;

                if (ocupado >= inventario.CapacidadeTotal)
                {
                    return BadRequest(
                        "A sua coleção está cheia (" + ocupado + "/" + inventario.CapacidadeTotal +
                        "). Liberte cópias repetidas na página da Coleção antes de invocar de novo.");
                }

                // Só considera raridades com pelo menos uma personagem ativa
                var raridades = await _contexto.Raridades
                    .Where(r => r.Personagens!.Any(p => p.IsAtivo))
                    .Include(r => r.Personagens!.Where(p => p.IsAtivo))
                    .OrderBy(r => r.Ordem)
                    .ToListAsync();

                if (raridades.Count == 0)
                {
                    return NotFound("Não existem personagens para invocar.");
                }

                // 1) Escolha ponderada da raridade
                //    As probabilidades são frações (ex.: 0.55 = 55%). O sorteio
                //    gera um número entre 0 e a soma dos pesos, e percorre as
                //    raridades acumulando até o ultrapassar.
                decimal somaPesos = raridades.Sum(r => r.Probabilidade);
                decimal sorteio = (decimal)Random.Shared.NextDouble() * somaPesos;

                Raridade raridadeEscolhida = raridades[0];
                decimal acumulado = 0;
                foreach (var raridade in raridades)
                {
                    acumulado += raridade.Probabilidade;
                    if (sorteio < acumulado)
                    {
                        raridadeEscolhida = raridade;
                        break;
                    }
                }

                // 2) Escolha aleatória da personagem dentro da raridade
                var candidatas = raridadeEscolhida.Personagens!.ToList();
                var personagem = candidatas[Random.Shared.Next(candidatas.Count)];

                // 3) Atualiza a coleção e regista a invocação no histórico.
                //    TRANSAÇÃO: são duas gravações (coleção e histórico) e têm
                //    de ficar coerentes — ou entram as duas, ou não entra nada.
                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                // 3a) Coleção: primeira vez cria a linha, repetida soma uma cópia
                //     (a base de dados garante UNIQUE por utilizador+personagem).
                var itemColecao = await _contexto.Colecoes.FirstOrDefaultAsync(
                    c => c.UtilizadorId == utilizador.Id && c.PersonagemId == personagem.Id);

                bool novo = itemColecao == null;

                if (itemColecao == null)
                {
                    itemColecao = new ItemColecao
                    {
                        UtilizadorId = utilizador.Id,
                        PersonagemId = personagem.Id,
                        Quantidade = 1,
                        IsFavorito = false,
                        PontosAmizade = 0,
                        NivelAmizadeId = NivelAmizadeInicialId,
                        DataObtencao = DateTime.UtcNow
                    };

                    _contexto.Colecoes.Add(itemColecao);
                }
                else
                {
                    itemColecao.Quantidade++;
                }

                try
                {
                    await _contexto.SaveChangesAsync();
                }
                catch (DbUpdateException) when (novo)
                {
                    // Duas invocações ao mesmo tempo (dois separadores, duplo
                    // clique): a outra criou a linha primeiro e o UNIQUE recusou
                    // esta. Em vez de dar erro, lê a linha que já existe e soma
                    // a cópia — o utilizador não perde a invocação.
                    _contexto.Entry(itemColecao).State = EntityState.Detached;

                    itemColecao = await _contexto.Colecoes.FirstAsync(
                        c => c.UtilizadorId == utilizador.Id && c.PersonagemId == personagem.Id);

                    itemColecao.Quantidade++;
                    novo = false;

                    await _contexto.SaveChangesAsync();
                }

                // 3b) Histórico da invocação
                var invocacao = new Invocacao
                {
                    UtilizadorId = utilizador.Id,
                    BannerId = BannerPermanenteId,
                    PersonagemId = personagem.Id,
                    RaridadeId = raridadeEscolhida.Id,
                    PityAtivado = false,
                    Data = DateTime.UtcNow
                };

                _contexto.Invocacoes.Add(invocacao);
                await _contexto.SaveChangesAsync();

                await transacao.CommitAsync();

                return Ok(new InvocacaoResultado
                {
                    Personagem = personagem,
                    Novo = novo,
                    Quantidade = itemColecao.Quantidade,
                    Ocupado = ocupado + 1,
                    Capacidade = inventario.CapacidadeTotal
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao realizar a invocação: " + ex.Message);
            }
        }

        /// <summary>
        /// Inventário do utilizador (espaço da coleção). O registo cria esta
        /// linha, mas as contas feitas por script (Admin, Sistema) podem não a
        /// ter: aqui — no caminho de escrita — é criada com a capacidade base.
        /// (Na consulta da coleção não se cria nada; ver ColecaoController.)
        /// </summary>
        private async Task<Inventario> ObterOuCriarInventarioAsync(int utilizadorId)
        {
            var inventario = await _contexto.Inventarios.FindAsync(utilizadorId);

            if (inventario == null)
            {
                // INSERT idempotente: se dois pedidos chegarem ao mesmo tempo,
                // o segundo não rebenta com erro de chave duplicada.
                await _contexto.Database.ExecuteSqlAsync(
                    $@"INSERT INTO InventarioUtilizador (UtilizadorId, CapacidadeBase, CapacidadeExtra)
                       SELECT {utilizadorId}, 100, 0
                       WHERE NOT EXISTS (SELECT 1 FROM InventarioUtilizador WHERE UtilizadorId = {utilizadorId})");

                inventario = await _contexto.Inventarios.FirstAsync(i => i.UtilizadorId == utilizadorId);
            }

            return inventario;
        }
    }
}
