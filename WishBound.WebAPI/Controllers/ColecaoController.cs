using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Coleção pessoal de cada utilizador (tabela ColecaoUtilizador):
    ///   - consulta da coleção com ordenação e filtro de favoritos (SELECT);
    ///   - detalhe de uma personagem da coleção (SELECT);
    ///   - marcar/desmarcar favorita (UPDATE);
    ///   - libertar cópias repetidas para ganhar espaço (UPDATE).
    ///
    /// As personagens entram aqui pelo sistema de invocação
    /// (ver InvocacoesController), que também verifica se ainda há espaço.
    ///
    /// ECONOMIA (parte usada pela coleção): libertar cópias repetidas dá
    /// "Moedas" conforme a raridade, e essas Moedas compram mais lugares para
    /// a coleção. Todos os movimentos ficam registados em TransacoesMoeda.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ColecaoController : ControllerBase
    {
        /// <summary>Moeda normal da plataforma (TiposMoeda: 1 = Gemas, 2 = Moedas).</summary>
        private const int MoedasId = 2;

        /// <summary>Lugares acrescentados por cada expansão comprada.</summary>
        private const int LugaresPorExpansao = 10;

        /// <summary>
        /// Preço da primeira expansão. As seguintes ficam progressivamente
        /// mais caras: 90, 180, 270... (preço = base x expansões já compradas + 1).
        /// </summary>
        private const int PrecoBaseExpansao = 90;

        private readonly WishBoundContext _contexto;

        public ColecaoController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>
        /// Moedas dadas por cada cópia repetida libertada, conforme a ordem da
        /// raridade (1 = Comum ... 5 = Mítico). Uma repetida mítica vale muito
        /// mais do que uma comum.
        /// </summary>
        private static decimal ValorPorRepetida(int ordemRaridade) => ordemRaridade switch
        {
            5 => 250m,
            4 => 120m,
            3 => 60m,
            2 => 25m,
            _ => 10m
        };

        // ------------------------------------------------------------
        // GET: api/colecao?utilizadorId=5&ordenar=raridade&favoritos=false
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<ColecaoResposta>> ObterColecao(
            [FromQuery] int utilizadorId,
            [FromQuery] string? ordenar = "raridade",
            [FromQuery] bool favoritos = false)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                var consulta = _contexto.Colecoes
                    .Where(c => c.UtilizadorId == utilizadorId)
                    .Include(c => c.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .AsQueryable();

                if (favoritos)
                {
                    consulta = consulta.Where(c => c.IsFavorito);
                }

                // ORDER BY feito na base de dados (não em memória)
                consulta = (ordenar ?? "raridade").ToLower() switch
                {
                    "nome" => consulta.OrderBy(c => c.Personagem!.Nome),
                    "data" => consulta.OrderByDescending(c => c.DataObtencao),
                    // Por omissão: das raridades mais altas para as mais baixas
                    _ => consulta
                        .OrderByDescending(c => c.Personagem!.Raridade!.Ordem)
                        .ThenBy(c => c.Personagem!.Nome)
                };

                var itens = await consulta.ToListAsync();

                var inventario = await ObterInventarioAsync(utilizadorId);

                // O espaço ocupado conta TODAS as cópias, incluindo as repetidas
                int ocupado = await _contexto.Colecoes
                    .Where(c => c.UtilizadorId == utilizadorId)
                    .SumAsync(c => (int?)c.Quantidade) ?? 0;

                int distintas = await _contexto.Colecoes
                    .CountAsync(c => c.UtilizadorId == utilizadorId && c.Personagem!.IsAtivo);

                return Ok(new ColecaoResposta
                {
                    Itens = itens.Select(ParaResposta).ToList(),
                    Ocupado = ocupado,
                    Capacidade = inventario.CapacidadeTotal,
                    PersonagensDistintas = distintas,
                    PersonagensExistentes = await _contexto.Personagens.CountAsync(p => p.IsAtivo),
                    SaldoMoedas = await ObterSaldoAsync(utilizadorId),
                    PrecoProximaExpansao = PrecoDaProximaExpansao(inventario),
                    LugaresPorExpansao = LugaresPorExpansao
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a coleção: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/colecao/item?utilizadorId=5&personagemId=3
        // ------------------------------------------------------------
        [HttpGet("item")]
        public async Task<ActionResult<ItemColecaoResposta>> ObterItem(
            [FromQuery] int utilizadorId, [FromQuery] int personagemId)
        {
            try
            {
                if (utilizadorId <= 0 || personagemId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador e a personagem.");
                }

                var item = await _contexto.Colecoes
                    .Include(c => c.Personagem)
                        .ThenInclude(p => p!.Raridade)
                    .FirstOrDefaultAsync(c => c.UtilizadorId == utilizadorId && c.PersonagemId == personagemId);

                if (item == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                return Ok(ParaResposta(item));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter a personagem da coleção: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/colecao/favorito  (UPDATE)
        // ------------------------------------------------------------
        [HttpPost("favorito")]
        public async Task<IActionResult> MarcarFavorito(FavoritoPedido pedido)
        {
            try
            {
                if (pedido.UtilizadorId <= 0 || pedido.PersonagemId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador e a personagem.");
                }

                var item = await _contexto.Colecoes.FirstOrDefaultAsync(
                    c => c.UtilizadorId == pedido.UtilizadorId && c.PersonagemId == pedido.PersonagemId);

                if (item == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                item.IsFavorito = pedido.Favorito;
                await _contexto.SaveChangesAsync();

                return Ok(pedido.Favorito
                    ? "Personagem marcada como favorita."
                    : "Personagem removida dos favoritos.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar o favorito: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/colecao/libertar  (UPDATE - gestão de duplicados)
        // ------------------------------------------------------------
        // Liberta cópias repetidas para recuperar espaço. A primeira cópia
        // nunca é libertada: a personagem continua na coleção.
        [HttpPost("libertar")]
        public async Task<IActionResult> LibertarDuplicados(LibertarPedido pedido)
        {
            try
            {
                if (pedido.UtilizadorId <= 0 || pedido.PersonagemId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador e a personagem.");
                }

                var item = await _contexto.Colecoes
                    .Include(c => c.Personagem)
                    .FirstOrDefaultAsync(
                        c => c.UtilizadorId == pedido.UtilizadorId && c.PersonagemId == pedido.PersonagemId);

                if (item == null)
                {
                    return NotFound("Esta personagem ainda não faz parte da coleção.");
                }

                int repetidas = item.Quantidade - 1;
                if (repetidas <= 0)
                {
                    return BadRequest("Só tem uma cópia desta personagem: não há repetidas para libertar.");
                }

                // 0 ou menos = libertar todas as repetidas
                int aLibertar = pedido.Quantidade <= 0
                    ? repetidas
                    : Math.Min(pedido.Quantidade, repetidas);

                // Cada repetida vale Moedas conforme a raridade da personagem
                var raridade = item.Personagem == null
                    ? null
                    : await _contexto.Raridades.FindAsync(item.Personagem.RaridadeId);

                decimal ganho = ValorPorRepetida(raridade?.Ordem ?? 1) * aLibertar;

                await GarantirCarteiraAsync(pedido.UtilizadorId);

                // TRANSAÇÃO: tirar as cópias, creditar a carteira e registar o
                // movimento têm de acontecer juntos.
                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                // As duas contas são feitas com UPDATE ... SET x = x ± n (e não
                // "ler, somar em memória, gravar"): se chegarem dois pedidos ao
                // mesmo tempo, nenhum escreve por cima do outro. O WHERE
                // garante ainda que só liberta quem tem mesmo essas repetidas.
                int linhas = await _contexto.Database.ExecuteSqlAsync(
                    $"UPDATE ColecaoUtilizador SET Quantidade = Quantidade - {aLibertar} WHERE ColecaoId = {item.Id} AND Quantidade > {aLibertar}");

                if (linhas == 0)
                {
                    return BadRequest("Já não existem essas cópias repetidas para libertar.");
                }

                await _contexto.Database.ExecuteSqlAsync(
                    $"UPDATE CarteirasUtilizador SET Saldo = Saldo + {ganho} WHERE UtilizadorId = {pedido.UtilizadorId} AND TipoMoedaId = {MoedasId}");

                _contexto.TransacoesMoeda.Add(new TransacaoMoeda
                {
                    UtilizadorId = pedido.UtilizadorId,
                    TipoMoedaId = MoedasId,
                    Montante = ganho,
                    TipoTransacao = TransacaoMoeda.TipoGanho,
                    Origem = "Libertar repetidas",
                    DataCriacao = DateTime.UtcNow
                });

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                decimal saldo = await ObterSaldoAsync(pedido.UtilizadorId);

                string quantas = aLibertar == 1
                    ? "Libertada 1 cópia repetida de " + (item.Personagem?.Nome ?? "personagem") + "."
                    : "Libertadas " + aLibertar + " cópias repetidas de " + (item.Personagem?.Nome ?? "personagem") + ".";

                return Ok(quantas + " Ganhou " + ganho.ToString("0") + " Moedas (saldo: " + saldo.ToString("0") + ").");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao libertar as cópias repetidas: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/colecao/expandir  (UPDATE - comprar mais lugares)
        // ------------------------------------------------------------
        // Cada expansão acrescenta 10 lugares e custa mais do que a anterior
        // (90, 180, 270...). Pago em Moedas — que se ganham a libertar repetidas.
        [HttpPost("expandir")]
        public async Task<IActionResult> ExpandirInventario(ExpandirPedido pedido)
        {
            try
            {
                if (pedido.UtilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador.");
                }

                var utilizador = await _contexto.Utilizadores.FindAsync(pedido.UtilizadorId);
                if (utilizador == null || !utilizador.IsAtivo)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                // Garante que as linhas existem (contas criadas por script podem
                // não as ter) antes de fazer as contas com UPDATE.
                await GarantirInventarioAsync(pedido.UtilizadorId);
                await GarantirCarteiraAsync(pedido.UtilizadorId);

                var inventario = await _contexto.Inventarios.FindAsync(pedido.UtilizadorId);
                decimal preco = PrecoDaProximaExpansao(inventario!);

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                // Débito condicional: o próprio UPDATE verifica o saldo. Se
                // devolver 0 linhas, não havia Moedas suficientes — e não fica
                // nada gravado (dois pedidos simultâneos não podem pagar uma
                // expansão cada com o mesmo saldo).
                int linhas = await _contexto.Database.ExecuteSqlAsync(
                    $"UPDATE CarteirasUtilizador SET Saldo = Saldo - {preco} WHERE UtilizadorId = {pedido.UtilizadorId} AND TipoMoedaId = {MoedasId} AND Saldo >= {preco}");

                if (linhas == 0)
                {
                    decimal saldoAtual = await ObterSaldoAsync(pedido.UtilizadorId);
                    return BadRequest(
                        "Moedas insuficientes: a próxima expansão custa " + preco.ToString("0") +
                        " e tem " + saldoAtual.ToString("0") +
                        ". Liberte cópias repetidas para ganhar Moedas.");
                }

                await _contexto.Database.ExecuteSqlAsync(
                    $"UPDATE InventarioUtilizador SET CapacidadeExtra = CapacidadeExtra + {LugaresPorExpansao} WHERE UtilizadorId = {pedido.UtilizadorId}");

                _contexto.TransacoesMoeda.Add(new TransacaoMoeda
                {
                    UtilizadorId = pedido.UtilizadorId,
                    TipoMoedaId = MoedasId,
                    Montante = preco,
                    TipoTransacao = TransacaoMoeda.TipoGasto,
                    Origem = "Expansao do inventario",
                    DataCriacao = DateTime.UtcNow
                });

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                // Relê os valores já gravados para a mensagem
                await _contexto.Entry(inventario!).ReloadAsync();
                decimal saldo = await ObterSaldoAsync(pedido.UtilizadorId);

                return Ok("Inventário expandido para " + inventario!.CapacidadeTotal +
                          " lugares. Gastou " + preco.ToString("0") +
                          " Moedas (saldo: " + saldo.ToString("0") + ").");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao expandir o inventário: " + ex.Message);
            }
        }

        // ============================================================
        //  Métodos auxiliares
        // ============================================================

        /// <summary>
        /// Preço da próxima expansão: sobe a cada compra
        /// (90 na primeira, 180 na segunda, 270 na terceira...).
        /// </summary>
        private static decimal PrecoDaProximaExpansao(Inventario inventario)
        {
            // Math.Max protege contra um valor negativo posto à mão na base de
            // dados (senão o preço podia dar 0 ou até negativo).
            int compradas = Math.Max(0, inventario.CapacidadeExtra) / LugaresPorExpansao;
            return PrecoBaseExpansao * (compradas + 1);
        }

        /// <summary>Saldo em Moedas (0 se ainda não tiver carteira).</summary>
        private async Task<decimal> ObterSaldoAsync(int utilizadorId)
        {
            var carteira = await _contexto.Carteiras.FindAsync(utilizadorId, MoedasId);
            return carteira?.Saldo ?? 0m;
        }

        /// <summary>
        /// Garante que o utilizador tem carteira de Moedas. O registo cria-a,
        /// mas as contas feitas por script (Sistema, Admin) podem não a ter.
        /// O INSERT ... WHERE NOT EXISTS é idempotente: dois pedidos ao mesmo
        /// tempo não dão erro de chave duplicada.
        /// </summary>
        private async Task GarantirCarteiraAsync(int utilizadorId)
        {
            await _contexto.Database.ExecuteSqlAsync(
                $@"INSERT INTO CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
                   SELECT {utilizadorId}, {MoedasId}, 0
                   WHERE NOT EXISTS (SELECT 1 FROM CarteirasUtilizador
                                     WHERE UtilizadorId = {utilizadorId} AND TipoMoedaId = {MoedasId})");
        }

        /// <summary>
        /// Garante que o utilizador tem linha de inventário (mesma ideia da
        /// carteira). Só é usado nos caminhos de escrita — a consulta da
        /// coleção não escreve nada (ver ObterInventarioAsync).
        /// </summary>
        private async Task GarantirInventarioAsync(int utilizadorId)
        {
            await _contexto.Database.ExecuteSqlAsync(
                $@"INSERT INTO InventarioUtilizador (UtilizadorId, CapacidadeBase, CapacidadeExtra)
                   SELECT {utilizadorId}, 100, 0
                   WHERE NOT EXISTS (SELECT 1 FROM InventarioUtilizador WHERE UtilizadorId = {utilizadorId})");
        }

        /// <summary>
        /// Inventário do utilizador. Isto é uma CONSULTA: se a linha ainda não
        /// existir (contas criadas por script), devolve a capacidade base sem
        /// escrever nada na base de dados — a linha é criada na primeira
        /// invocação (ver InvocacoesController).
        /// </summary>
        private async Task<Inventario> ObterInventarioAsync(int utilizadorId)
        {
            return await _contexto.Inventarios.FindAsync(utilizadorId)
                   ?? new Inventario { UtilizadorId = utilizadorId, CapacidadeBase = 100, CapacidadeExtra = 0 };
        }

        /// <summary>Converte a linha da coleção no DTO enviado ao site.</summary>
        private static ItemColecaoResposta ParaResposta(ItemColecao item)
        {
            return new ItemColecaoResposta
            {
                PersonagemId = item.PersonagemId,
                Nome = item.Personagem?.Nome ?? "Personagem removida",
                Descricao = item.Personagem?.Descricao,
                ImagemUrl = item.Personagem?.ImagemUrl,
                RaridadeId = item.Personagem?.RaridadeId ?? 0,
                RaridadeNome = item.Personagem?.Raridade?.Nome ?? "Desconhecida",
                RaridadeCor = item.Personagem?.Raridade?.Cor,
                RaridadeOrdem = item.Personagem?.Raridade?.Ordem ?? 0,
                Quantidade = item.Quantidade,
                IsFavorito = item.IsFavorito,
                DataObtencao = item.DataObtencao
            };
        }
    }
}
