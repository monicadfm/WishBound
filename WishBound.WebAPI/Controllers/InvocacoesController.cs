using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;
using WishBound.WebAPI.Services;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Sistema de invocação (gacha).
    ///
    /// Cada invocação pertence a um utilizador real e a um BANNER escolhido
    /// pelo site (já não há Id fixo no código): o banner define que
    /// personagens podem sair e é nele que se contam as garantias.
    ///
    /// GARANTIAS (pity), contadas por utilizador e por banner:
    ///   - Épica ou melhor a cada 10 invocações sem nenhuma (por isso uma
    ///     invocação de 10 traz sempre pelo menos uma Épica);
    ///   - Lendária ou Mítica aos 90. Até à 80 as probabilidades são as
    ///     normais (3% no total); da 81 em diante sobem 10 pontos por
    ///     invocação (81 = 13%, 85 = 53%, 89 = 93%) até serem garantidas na 90.
    ///
    /// Cada invocação custa 10 Moedas (uma x10 custa 100) — ou 1 BILHETE de
    /// invocação (moeda 3, vindo da recompensa diária e dos eventos): os
    /// bilhetes são gastos primeiro e as Moedas pagam só o que sobrar (uma
    /// x10 com 4 bilhetes custa 4 bilhetes + 60 Moedas). O pagamento é feito
    /// com UPDATEs condicionais nas carteiras e fica registado em
    /// TransacoesMoeda.
    ///
    /// A personagem obtida entra na COLEÇÃO (primeira vez cria a linha,
    /// repetida soma uma cópia) e cada cópia ocupa um lugar do inventário —
    /// sem espaço (ou sem Moedas) para todas as invocações pedidas, o pedido é
    /// recusado sem gastar nada.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InvocacoesController : ControllerBase
    {
        /// <summary>Invocações até à garantia de Lendária/Mítica.</summary>
        private const int LimiteLendario = 90;

        /// <summary>A partir daqui a probabilidade de Lendária/Mítica sobe.</summary>
        private const int InicioSoftPity = 81;

        /// <summary>Quanto sobe por cada invocação depois do início do soft pity.</summary>
        private const decimal AumentoSoftPity = 0.10m;

        /// <summary>Invocações até à garantia de Épica.</summary>
        private const int LimiteEpico = 10;

        private const int OrdemEpico = 3;
        private const int OrdemLendario = 4;

        /// <summary>Primeiro nível de amizade ("Desconhecido") ao criar a linha da coleção.</summary>
        private const int NivelAmizadeInicialId = 1;

        /// <summary>Moeda normal (ver TiposMoedaIds).</summary>
        private const int MoedasId = TiposMoedaIds.Moedas;

        /// <summary>Bilhetes de invocação: cada um paga uma invocação inteira.</summary>
        private const int BilhetesId = TiposMoedaIds.Bilhetes;

        /// <summary>Custo de cada invocação, em Moedas (uma x10 custa 100).</summary>
        private const int CustoInvocacao = 10;

        private readonly WishBoundContext _contexto;
        private readonly ServicoAmizade _amizade;

        public InvocacoesController(WishBoundContext contexto, ServicoAmizade amizade)
        {
            _contexto = contexto;
            _amizade = amizade;
        }

        // ------------------------------------------------------------
        // GET: api/invocacoes?utilizadorId=5   (histórico DO utilizador)
        // ------------------------------------------------------------
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

        // ------------------------------------------------------------
        // GET: api/invocacoes/estado?utilizadorId=5&bannerId=1
        // ------------------------------------------------------------
        // Estado dos contadores de garantia, para a página mostrar quanto
        // falta antes de o utilizador invocar.
        [HttpGet("estado")]
        public async Task<ActionResult<EstadoPityResposta>> ObterEstado(
            [FromQuery] int utilizadorId, [FromQuery] int bannerId = 0)
        {
            try
            {
                if (utilizadorId <= 0)
                {
                    return BadRequest("É necessário indicar o utilizador (utilizadorId).");
                }

                var banner = await ObterBannerAsync(bannerId);
                if (banner == null)
                {
                    return NotFound("Não há nenhum banner disponível.");
                }

                var pity = await _contexto.Pity.FindAsync(utilizadorId, banner.Id);
                int contadorLendario = pity?.ContadorAtual ?? 0;
                int contadorEpico = pity?.ContadorEpico ?? 0;

                var dono = await _contexto.Utilizadores.AsNoTracking()
                    .Where(u => u.Id == utilizadorId)
                    .Select(u => new { u.UltimoLoginDiario })
                    .FirstOrDefaultAsync();

                return Ok(new EstadoPityResposta
                {
                    SaldoMoedas = await ObterSaldoAsync(utilizadorId),
                    SaldoBilhetes = await ObterSaldoAsync(utilizadorId, BilhetesId),
                    RecompensaDiariaDisponivel = dono != null &&
                        dono.UltimoLoginDiario != DateOnly.FromDateTime(DateTime.UtcNow),
                    CustoInvocacao = CustoInvocacao,
                    BannerId = banner.Id,
                    BannerNome = banner.Nome,
                    ContadorLendario = contadorLendario,
                    ContadorEpico = contadorEpico,
                    FaltamParaLendario = Math.Max(0, LimiteLendario - contadorLendario),
                    FaltamParaEpico = Math.Max(0, LimiteEpico - contadorEpico),
                    LimiteLendario = LimiteLendario,
                    LimiteEpico = LimiteEpico,
                    InicioSoftPity = InicioSoftPity
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o estado das garantias: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/invocacoes   (1 ou 10 invocações)
        // ------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<InvocacaoResultado>> Invocar([FromBody] InvocacaoPedido pedido)
        {
            try
            {
                if (pedido.Quantidade != 1 && pedido.Quantidade != 10)
                {
                    return BadRequest("Só é possível invocar 1 ou 10 de cada vez.");
                }

                int quantidade = pedido.Quantidade;

                // O utilizador tem de existir e estar ativo — protege contra
                // pedidos com Ids inventados (o histórico tem FK para Utilizadores).
                var utilizador = await _contexto.Utilizadores.FindAsync(pedido.UtilizadorId);
                if (utilizador == null || !utilizador.IsAtivo)
                {
                    return BadRequest("Utilizador inválido para invocar.");
                }

                var banner = await ObterBannerAsync(pedido.BannerId);
                if (banner == null)
                {
                    return BadRequest("O banner escolhido não existe ou já não está a decorrer.");
                }

                // TRANSAÇÃO a partir daqui: a verificação de espaço, os
                // contadores de garantia e as gravações têm de ver — e deixar —
                // um estado coerente, mesmo com dois pedidos ao mesmo tempo.
                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                // ESPAÇO NA COLEÇÃO: cada cópia (incluindo as repetidas) ocupa
                // um lugar. Sem espaço para TODAS as invocações pedidas, nada é
                // sorteado — o utilizador não perde invocações.
                var inventario = await ObterOuCriarInventarioAsync(utilizador.Id);

                int ocupado = await _contexto.Colecoes
                    .Where(c => c.UtilizadorId == utilizador.Id)
                    .SumAsync(c => (int?)c.Quantidade) ?? 0;

                if (!utilizador.IsAdmin && ocupado + quantidade > inventario.CapacidadeTotal)
                {
                    return BadRequest(
                        "Não há espaço para " + quantidade + (quantidade == 1 ? " invocação" : " invocações") +
                        " (" + ocupado + "/" + inventario.CapacidadeTotal +
                        "). Liberte cópias repetidas ou compre mais lugares na página da Coleção.");
                }

                // PAGAMENTO — primeiro os BILHETES (cada um paga uma invocação),
                // depois as Moedas para o que sobrar. Os UPDATEs só descontam se
                // houver saldo suficiente; se o das Moedas devolver 0 linhas,
                // nada é sorteado e a transação é desfeita ao sair (os bilhetes
                // descontados voltam também).
                await GarantirCarteiraAsync(utilizador.Id, MoedasId);
                await GarantirCarteiraAsync(utilizador.Id, BilhetesId);

                // ADMIN: invocações ilimitadas — não gasta bilhetes nem Moedas
                // (nada fica em TransacoesMoeda; o histórico de invocações fica).
                int bilhetesUsados = utilizador.IsAdmin
                    ? 0
                    : (int)Math.Min(quantidade, Math.Floor(await ObterSaldoAsync(utilizador.Id, BilhetesId)));

                if (bilhetesUsados > 0)
                {
                    int descontados = await _contexto.Database.ExecuteSqlAsync(
                        $@"UPDATE CarteirasUtilizador SET Saldo = Saldo - {bilhetesUsados}
                           WHERE UtilizadorId = {utilizador.Id} AND TipoMoedaId = {BilhetesId} AND Saldo >= {bilhetesUsados}");

                    // Outro pedido gastou os bilhetes entretanto: paga-se tudo em Moedas
                    if (descontados == 0)
                    {
                        bilhetesUsados = 0;
                    }
                }

                decimal custoTotal = utilizador.IsAdmin ? 0 : CustoInvocacao * (quantidade - bilhetesUsados);

                if (custoTotal > 0)
                {
                    int pagas = await _contexto.Database.ExecuteSqlAsync(
                        $@"UPDATE CarteirasUtilizador SET Saldo = Saldo - {custoTotal}
                           WHERE UtilizadorId = {utilizador.Id} AND TipoMoedaId = {MoedasId} AND Saldo >= {custoTotal}");

                    if (pagas == 0)
                    {
                        decimal saldoAtual = await ObterSaldoAsync(utilizador.Id);
                        string custo = bilhetesUsados > 0
                            ? "além dos " + bilhetesUsados + (bilhetesUsados == 1 ? " bilhete faltam " : " bilhetes faltam ") + custoTotal.ToString("0") + " Moedas"
                            : (quantidade == 1 ? "uma invocação custa " : "uma invocação x10 custa ") + custoTotal.ToString("0");
                        return BadRequest(
                            "Moedas insuficientes: " + custo + " e tem " + saldoAtual.ToString("0") +
                            ". Recebe a recompensa diária na Carteira ou liberte cópias repetidas na Coleção para ganhar Moedas.");
                    }
                }

                string sufixoOrigem = quantidade == 1 ? "Invocacao" : "Invocacao x10";

                if (bilhetesUsados > 0)
                {
                    _contexto.TransacoesMoeda.Add(new TransacaoMoeda
                    {
                        UtilizadorId = utilizador.Id,
                        TipoMoedaId = BilhetesId,
                        Montante = bilhetesUsados,
                        TipoTransacao = TransacaoMoeda.TipoGasto,
                        Origem = sufixoOrigem + " (bilhetes)",
                        DataCriacao = DateTime.UtcNow
                    });
                }

                if (custoTotal > 0)
                {
                    _contexto.TransacoesMoeda.Add(new TransacaoMoeda
                    {
                        UtilizadorId = utilizador.Id,
                        TipoMoedaId = MoedasId,
                        Montante = custoTotal,
                        TipoTransacao = TransacaoMoeda.TipoGasto,
                        Origem = sufixoOrigem,
                        DataCriacao = DateTime.UtcNow
                    });
                }

                // Personagens ativas deste banner, agrupadas por raridade
                var idsDoBanner = await _contexto.BannerPersonagens
                    .Where(bp => bp.BannerId == banner.Id)
                    .Select(bp => bp.PersonagemId)
                    .ToListAsync();

                var personagens = await _contexto.Personagens
                    .Include(p => p.Raridade)
                    .Where(p => p.IsAtivo && idsDoBanner.Contains(p.Id))
                    .ToListAsync();

                if (personagens.Count == 0)
                {
                    return NotFound("Este banner não tem personagens para invocar.");
                }

                // Personagens agrupadas pelo Id da raridade
                var porRaridade = personagens
                    .Where(p => p.Raridade != null)
                    .GroupBy(p => p.RaridadeId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var raridades = personagens
                    .Where(p => p.Raridade != null)
                    .Select(p => p.Raridade!)
                    .DistinctBy(r => r.Id)
                    .OrderBy(r => r.Ordem)
                    .ToList();

                if (raridades.Count == 0)
                {
                    return NotFound("As personagens deste banner não têm raridade associada.");
                }

                // As garantias são definidas em função do que ESTE banner tem.
                // Num banner sem Lendária/Mítica, a "raridade alta" garantida
                // passa a ser a melhor que existir (e a de baixo ajusta-se),
                // senão os contadores nunca poderiam ser reiniciados.
                int ordemAlta = raridades.Any(r => r.Ordem >= OrdemLendario)
                    ? OrdemLendario
                    : raridades.Max(r => r.Ordem);

                int? melhorAbaixo = raridades
                    .Where(r => r.Ordem < ordemAlta)
                    .Select(r => (int?)r.Ordem)
                    .Max();

                int ordemGarantida = melhorAbaixo.HasValue
                    ? Math.Min(OrdemEpico, melhorAbaixo.Value)
                    : ordemAlta;

                // Contadores de garantia deste utilizador NESTE banner
                var pity = await ObterOuCriarPityAsync(utilizador.Id, banner.Id);

                int contadorLendario = pity.ContadorAtual;
                int contadorEpico = pity.ContadorEpico;
                int? ultimaGarantida = pity.UltimaRaridadeGarantida;

                // Quantidades que já tem na coleção (para saber o que é novo)
                var colecaoAtual = await _contexto.Colecoes
                    .Where(c => c.UtilizadorId == utilizador.Id)
                    .ToDictionaryAsync(c => c.PersonagemId, c => c.Quantidade);

                var obtidas = new List<PersonagemObtida>();
                var sorteadasPorPersonagem = new Dictionary<int, int>();

                for (int i = 0; i < quantidade; i++)
                {
                    contadorLendario++;
                    contadorEpico++;

                    var (raridade, porGarantia) = SortearRaridade(
                        raridades, contadorLendario, contadorEpico, ordemAlta, ordemGarantida);
                    var candidatas = porRaridade[raridade.Id];
                    var personagem = candidatas[Random.Shared.Next(candidatas.Count)];

                    // Contadores: a raridade alta reinicia os dois
                    if (raridade.Ordem >= ordemAlta)
                    {
                        contadorLendario = 0;
                        contadorEpico = 0;
                    }
                    else if (raridade.Ordem >= ordemGarantida)
                    {
                        contadorEpico = 0;
                    }

                    if (porGarantia)
                    {
                        ultimaGarantida = raridade.Id;
                    }

                    sorteadasPorPersonagem.TryGetValue(personagem.Id, out int jaSorteadas);
                    sorteadasPorPersonagem[personagem.Id] = jaSorteadas + 1;

                    colecaoAtual.TryGetValue(personagem.Id, out int tinha);

                    obtidas.Add(new PersonagemObtida
                    {
                        Personagem = personagem,
                        Novo = tinha == 0 && jaSorteadas == 0,
                        Quantidade = tinha + jaSorteadas + 1,
                        PityAtivado = porGarantia,
                        RaridadeOrdem = raridade.Ordem
                    });
                }

                // ---------- Gravação ----------
                foreach (var par in sorteadasPorPersonagem)
                {
                    // UPDATE + INSERT condicional: soma à linha existente ou
                    // cria-a. Feito em SQL para a soma ser atómica.
                    await _contexto.Database.ExecuteSqlAsync(
                        $@"UPDATE ColecaoUtilizador SET Quantidade = Quantidade + {par.Value}
                           WHERE UtilizadorId = {utilizador.Id} AND PersonagemId = {par.Key};

                           INSERT INTO ColecaoUtilizador
                               (UtilizadorId, PersonagemId, Quantidade, IsFavorito, PontosAmizade, NivelAmizadeId, DataObtencao)
                           SELECT {utilizador.Id}, {par.Key}, {par.Value}, 0, 0, {NivelAmizadeInicialId}, SYSUTCDATETIME()
                           WHERE NOT EXISTS (SELECT 1 FROM ColecaoUtilizador
                                             WHERE UtilizadorId = {utilizador.Id} AND PersonagemId = {par.Key});");
                }

                // ---------- Amizade: as cópias REPETIDAS dão pontos ----------
                // Cada repetida vale pontos conforme a raridade (o dobro num
                // banner de evento). A primeira cópia de uma personagem nova
                // não dá pontos — a amizade começa aí, em "Desconhecido".
                var mensagensAmizade = new List<string>();
                int multiplicador = banner.TipoBanner == Banner.TipoEvento ? ServicoAmizade.MultiplicadorEvento : 1;

                foreach (var par in sorteadasPorPersonagem)
                {
                    int repetidas = par.Value - (colecaoAtual.ContainsKey(par.Key) ? 0 : 1);
                    if (repetidas <= 0)
                    {
                        continue;
                    }

                    var daPersonagem = obtidas.Where(o => o.Personagem!.Id == par.Key).ToList();
                    int pontosPorCopia = ServicoAmizade.PontosPorRepetida(daPersonagem[0].RaridadeOrdem) * multiplicador;

                    var resultadoAmizade = await _amizade.AdicionarPontosAsync(
                        utilizador.Id, par.Key, pontosPorCopia * repetidas, marcarInteracao: false);

                    if (resultadoAmizade == null)
                    {
                        continue;
                    }

                    string nomePersonagem = daPersonagem[0].Personagem!.Nome;

                    var recompensas = await _amizade.DesbloquearRecompensasAsync(
                        utilizador.Id, par.Key, nomePersonagem, resultadoAmizade.NivelAnterior, resultadoAmizade.NivelAtual);

                    foreach (var obtida in daPersonagem)
                    {
                        obtida.NivelAmizadeNome = resultadoAmizade.NivelAtual.Nome;
                        if (!obtida.Novo)
                        {
                            obtida.PontosAmizadeGanhos = pontosPorCopia;
                        }
                    }

                    if (resultadoAmizade.SubiuDeNivel)
                    {
                        daPersonagem[^1].SubiuDeNivel = true;

                        string mensagem = "A tua amizade com " + nomePersonagem + " subiu para \"" +
                                          resultadoAmizade.NivelAtual.Nome + "\"!";
                        if (recompensas.Count > 0)
                        {
                            mensagem += " Desbloqueaste: " + string.Join(", ", recompensas.Select(ServicoAmizade.DescreverRecompensa)) + ".";
                        }
                        mensagensAmizade.Add(mensagem);
                    }
                }

                foreach (var obtida in obtidas)
                {
                    _contexto.Invocacoes.Add(new Invocacao
                    {
                        UtilizadorId = utilizador.Id,
                        BannerId = banner.Id,
                        PersonagemId = obtida.Personagem!.Id,
                        RaridadeId = obtida.Personagem.RaridadeId,
                        PityAtivado = obtida.PityAtivado,
                        Data = DateTime.UtcNow
                    });
                }

                pity.ContadorAtual = contadorLendario;
                pity.ContadorEpico = contadorEpico;
                pity.UltimaRaridadeGarantida = ultimaGarantida;

                await _contexto.SaveChangesAsync();
                await transacao.CommitAsync();

                return Ok(new InvocacaoResultado
                {
                    Personagens = obtidas,
                    MensagensAmizade = mensagensAmizade,
                    BannerId = banner.Id,
                    BannerNome = banner.Nome,
                    Ocupado = ocupado + quantidade,
                    Capacidade = inventario.CapacidadeTotal,
                    ContadorLendario = contadorLendario,
                    ContadorEpico = contadorEpico,
                    FaltamParaLendario = Math.Max(0, LimiteLendario - contadorLendario),
                    FaltamParaEpico = Math.Max(0, LimiteEpico - contadorEpico),
                    SaldoMoedas = await ObterSaldoAsync(utilizador.Id),
                    SaldoBilhetes = await ObterSaldoAsync(utilizador.Id, BilhetesId),
                    CustoTotal = custoTotal,
                    BilhetesUsados = bilhetesUsados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao realizar a invocação: " + ex.Message);
            }
        }

        // ============================================================
        //  Sorteio
        // ============================================================

        /// <summary>
        /// Escolhe a raridade de UMA invocação, aplicando as garantias.
        /// Devolve também se saiu por garantia (pity) ou pela sorte normal.
        /// </summary>
        private static (Raridade Raridade, bool PorGarantia) SortearRaridade(
            List<Raridade> raridades, int contadorLendario, int contadorEpico,
            int ordemAlta, int ordemGarantida)
        {
            // ordemAlta / ordemGarantida vêm do que o banner tem mesmo, para
            // as garantias serem sempre cumpríveis (ver o método Invocar).
            var altas = raridades.Where(r => r.Ordem >= ordemAlta).ToList();
            var garantidas = raridades.Where(r => r.Ordem == ordemGarantida).ToList();
            var normais = raridades.Where(r => r.Ordem < ordemAlta).ToList();

            // Garantia dos 90
            if (contadorLendario >= LimiteLendario)
            {
                return (SortearPorPeso(altas), true);
            }

            // Probabilidade normal das altas (3%), com soft pity a partir da 81
            decimal probabilidadeAltas = altas.Sum(r => r.Probabilidade);
            if (contadorLendario >= InicioSoftPity)
            {
                probabilidadeAltas = Math.Min(1m,
                    probabilidadeAltas + AumentoSoftPity * (contadorLendario - InicioSoftPity + 1));
            }

            if ((decimal)Random.Shared.NextDouble() < probabilidadeAltas)
            {
                // Só conta como garantia se veio da fase de soft pity
                return (SortearPorPeso(altas), contadorLendario >= InicioSoftPity);
            }

            // Garantia dos 10 (Épica, ou a melhor abaixo da alta neste banner)
            if (contadorEpico >= LimiteEpico && garantidas.Count > 0)
            {
                return (SortearPorPeso(garantidas), true);
            }

            // Sorteio normal entre as raridades abaixo de Lendária
            return (SortearPorPeso(normais.Count > 0 ? normais : raridades), false);
        }

        /// <summary>
        /// Sorteio ponderado: as probabilidades são frações (0.55 = 55%) e
        /// funcionam como pesos — sorteia-se um número entre 0 e a soma dos
        /// pesos e percorrem-se as raridades até o ultrapassar.
        /// </summary>
        private static Raridade SortearPorPeso(List<Raridade> raridades)
        {
            decimal soma = raridades.Sum(r => r.Probabilidade);

            if (soma <= 0)
            {
                return raridades[Random.Shared.Next(raridades.Count)];
            }

            decimal sorteio = (decimal)Random.Shared.NextDouble() * soma;
            decimal acumulado = 0;

            foreach (var raridade in raridades)
            {
                acumulado += raridade.Probabilidade;
                if (sorteio < acumulado)
                {
                    return raridade;
                }
            }

            return raridades[^1];
        }

        // ============================================================
        //  Métodos auxiliares
        // ============================================================

        /// <summary>
        /// Banner pedido (se estiver a decorrer) ou, sem Id, o banner
        /// permanente ativo. Substitui o antigo Id fixo no código.
        /// </summary>
        private async Task<Banner?> ObterBannerAsync(int bannerId)
        {
            var agora = DateTime.UtcNow;

            if (bannerId > 0)
            {
                return await _contexto.Banners.FirstOrDefaultAsync(
                    b => b.Id == bannerId && b.IsAtivo && b.DataInicio <= agora && b.DataFim >= agora);
            }

            return await _contexto.Banners
                .Where(b => b.IsAtivo && b.DataInicio <= agora && b.DataFim >= agora)
                .OrderBy(b => b.TipoBanner == Banner.TipoStandard ? 0 : 1)
                .ThenBy(b => b.Id)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Saldo do utilizador numa moeda (Moedas por omissão; 0 se ainda não
        /// tiver carteira). Lido sempre da base de dados — depois dos UPDATEs
        /// diretos, o valor em memória do EF estaria desatualizado.
        /// </summary>
        private async Task<decimal> ObterSaldoAsync(int utilizadorId, int tipoMoedaId = MoedasId)
        {
            return await _contexto.Carteiras.AsNoTracking()
                .Where(c => c.UtilizadorId == utilizadorId && c.TipoMoedaId == tipoMoedaId)
                .Select(c => c.Saldo)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Garante que existe carteira da moeda indicada (contas criadas por
        /// script podem não a ter). INSERT idempotente: dois pedidos ao mesmo
        /// tempo não dão erro de chave duplicada.
        /// </summary>
        private async Task GarantirCarteiraAsync(int utilizadorId, int tipoMoedaId)
        {
            await _contexto.Database.ExecuteSqlAsync(
                $@"INSERT INTO CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo)
                   SELECT {utilizadorId}, {tipoMoedaId}, 0
                   WHERE NOT EXISTS (SELECT 1 FROM CarteirasUtilizador
                                     WHERE UtilizadorId = {utilizadorId} AND TipoMoedaId = {tipoMoedaId})");
        }

        /// <summary>Contadores de garantia do utilizador neste banner (cria se faltar).</summary>
        private async Task<Pity> ObterOuCriarPityAsync(int utilizadorId, int bannerId)
        {
            // WITH (UPDLOCK, HOLDLOCK): dentro da transação, bloqueia a linha
            // para dois pedidos do mesmo utilizador não lerem os contadores ao
            // mesmo tempo e um apagar o trabalho do outro.
            var pity = await _contexto.Pity
                .FromSql($@"SELECT UtilizadorId, BannerId, ContadorAtual, ContadorEpico, UltimaRaridadeGarantida
                            FROM PityUtilizador WITH (UPDLOCK, HOLDLOCK)
                            WHERE UtilizadorId = {utilizadorId} AND BannerId = {bannerId}")
                .FirstOrDefaultAsync();

            if (pity == null)
            {
                pity = new Pity
                {
                    UtilizadorId = utilizadorId,
                    BannerId = bannerId,
                    ContadorAtual = 0,
                    ContadorEpico = 0
                };

                _contexto.Pity.Add(pity);
            }

            return pity;
        }

        /// <summary>
        /// Inventário do utilizador (espaço da coleção). O registo cria esta
        /// linha, mas as contas feitas por script (Admin, Sistema) podem não a
        /// ter: aqui — no caminho de escrita — é criada com a capacidade base.
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
