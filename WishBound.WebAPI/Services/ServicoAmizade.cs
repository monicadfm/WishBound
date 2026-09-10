using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Services
{
    /// <summary>
    /// Regras do SISTEMA DE AMIZADE, partilhadas pelo AmizadeController
    /// (interações) e pelo InvocacoesController (cópias repetidas).
    ///
    /// PONTOS:
    ///   - INTERAÇÕES: 3 por dia (dia UTC), +25 cada, em qualquer personagem
    ///     (até as 3 na mesma). Entrar todos os dias é o que as garante;
    ///   - REPETIDAS: cada cópia repetida obtida numa invocação dá pontos
    ///     conforme a raridade (Comum 10 · Raro 20 · Épico 40 · Lendário 80
    ///     · Mítico 160); num banner de EVENTO dão o dobro.
    ///
    /// NÍVEIS (NiveisAmizade, 7): Desconhecido 0 → Conhecido 100 →
    /// Melhor Amigo 300 → Confidente 700 → Inseparável 1500 →
    /// Laço Especial 3000 → Alma Gémea 5000. Cada personagem só sobe até
    /// (ordem da raridade + 2): Comum 3 · Raro 4 · Épico 5 · Lendário 6 ·
    /// Mítico 7 — os pontos ficam limitados ao limiar desse nível. Tudo
    /// recalculado em SQL a partir das tabelas, para se poder afinar na BD.
    ///
    /// RECOMPENSAS POR NÍVEL (INSERTs idempotentes):
    ///   3 - título "Melhor Amigo de X" (todas);
    ///   4 - notificações personalizadas da personagem (uma por dia, na
    ///       primeira interação do dia — tabela Notificacoes);
    ///   5 - emblema ("rebento") da personagem, equipável no perfil (até 3);
    ///   6 - título único na cor da raridade (Lendário/Mítico);
    ///   7 - moldura de perfil (Mítico).
    /// Cada subida de nível grava também uma notificação.
    /// </summary>
    public class ServicoAmizade
    {
        public const int InteracoesPorDia = 3;
        public const int PontosPorInteracao = 25;
        public const int MultiplicadorEvento = 2;

        /// <summary>Número máximo de emblemas equipados no perfil.</summary>
        public const int MaximoEmblemasEquipados = 3;

        public const int NivelTitulo = 3;
        public const int NivelNotificacoes = 4;
        public const int NivelEmblema = 5;
        public const int NivelTituloUnico = 6;
        public const int NivelMoldura = 7;

        private readonly WishBoundContext _contexto;
        private readonly ServicoMensagens _mensagens;

        public ServicoAmizade(WishBoundContext contexto, ServicoMensagens mensagens)
        {
            _contexto = contexto;
            _mensagens = mensagens;
        }

        /// <summary>Pontos por cópia repetida obtida, conforme a ordem da raridade (1 = Comum ... 5 = Mítico).</summary>
        public static int PontosPorRepetida(int ordemRaridade) => ordemRaridade switch
        {
            5 => 160,   // Mítico
            4 => 80,    // Lendário
            3 => 40,    // Épico
            2 => 20,    // Raro
            _ => 10     // Comum
        };

        /// <summary>Tabela dos pontos por repetida (para o site explicar as regras).</summary>
        public static Dictionary<int, int> TabelaPontosPorRepetida() =>
            Enumerable.Range(1, 5).ToDictionary(o => o, PontosPorRepetida);

        /// <summary>Nível máximo que uma personagem desta raridade pode atingir (Comum 3 ... Mítico 7).</summary>
        public static int NivelMaximo(int ordemRaridade) => Math.Clamp(ordemRaridade + 2, 3, 7);

        /// <summary>O que cada nível desbloqueia (texto para o site).</summary>
        public static string DescricaoRecompensa(int ordemNivel) => ordemNivel switch
        {
            NivelTitulo => "Título \"Melhor Amigo de …\"",
            NivelNotificacoes => "Notificações personalizadas da personagem",
            NivelEmblema => "Emblema da personagem para o perfil (até 3 equipados)",
            NivelTituloUnico => "Título único na cor da raridade",
            NivelMoldura => "Moldura de perfil",
            _ => ""
        };

        /// <summary>Resultado de somar pontos a uma personagem.</summary>
        public class ResultadoPontos
        {
            public int PontosAmizade { get; set; }
            public NivelAmizade NivelAnterior { get; set; } = new NivelAmizade();
            public NivelAmizade NivelAtual { get; set; } = new NivelAmizade();
            public bool SubiuDeNivel => NivelAtual.Ordem > NivelAnterior.Ordem;
        }

        /// <summary>Os níveis, do primeiro ao último.</summary>
        public async Task<List<NivelAmizade>> ObterNiveisAsync()
        {
            return await _contexto.NiveisAmizade.AsNoTracking()
                .OrderBy(n => n.Ordem)
                .ToListAsync();
        }

        /// <summary>
        /// Soma pontos de amizade à linha da coleção do utilizador com a
        /// personagem, limita-os ao máximo da raridade e recalcula o nível —
        /// tudo em SQL (soma atómica). A linha da coleção tem de existir.
        /// Deve correr dentro da transação de quem chama. Devolve o nível
        /// antes e depois, para quem chama desbloquear as recompensas.
        /// </summary>
        public async Task<ResultadoPontos?> AdicionarPontosAsync(int utilizadorId, int personagemId, int pontos, bool marcarInteracao)
        {
            var niveis = await ObterNiveisAsync();

            var antes = await _contexto.Colecoes.AsNoTracking()
                .Where(c => c.UtilizadorId == utilizadorId && c.PersonagemId == personagemId)
                .Select(c => new { c.PontosAmizade, c.NivelAmizadeId })
                .FirstOrDefaultAsync();

            if (antes == null)
            {
                return null;
            }

            if (marcarInteracao)
            {
                var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
                await _contexto.Database.ExecuteSqlAsync(
                    $@"UPDATE ColecaoUtilizador
                       SET PontosAmizade = PontosAmizade + {pontos}, UltimaInteracao = {hoje}
                       WHERE UtilizadorId = {utilizadorId} AND PersonagemId = {personagemId}");
            }
            else
            {
                await _contexto.Database.ExecuteSqlAsync(
                    $@"UPDATE ColecaoUtilizador
                       SET PontosAmizade = PontosAmizade + {pontos}
                       WHERE UtilizadorId = {utilizadorId} AND PersonagemId = {personagemId}");
            }

            // Limite: os pontos não passam do limiar do nível máximo da raridade
            // (ordem da raridade + 2), e o nível é o mais alto já atingido
            // dentro desse limite.
            await _contexto.Database.ExecuteSqlAsync(
                $@"UPDATE c
                   SET c.PontosAmizade = CASE WHEN c.PontosAmizade > m.PontosNecessarios THEN m.PontosNecessarios ELSE c.PontosAmizade END
                   FROM ColecaoUtilizador c
                   INNER JOIN Personagens p ON p.PersonagemId = c.PersonagemId
                   INNER JOIN Raridades r ON r.RaridadeId = p.RaridadeId
                   INNER JOIN NiveisAmizade m ON m.Ordem = r.Ordem + 2
                   WHERE c.UtilizadorId = {utilizadorId} AND c.PersonagemId = {personagemId}");

            await _contexto.Database.ExecuteSqlAsync(
                $@"UPDATE c
                   SET c.NivelAmizadeId = n.NivelAmizadeId
                   FROM ColecaoUtilizador c
                   INNER JOIN Personagens p ON p.PersonagemId = c.PersonagemId
                   INNER JOIN Raridades r ON r.RaridadeId = p.RaridadeId
                   CROSS APPLY (SELECT TOP 1 NivelAmizadeId
                                FROM NiveisAmizade
                                WHERE PontosNecessarios <= c.PontosAmizade AND Ordem <= r.Ordem + 2
                                ORDER BY Ordem DESC) n
                   WHERE c.UtilizadorId = {utilizadorId} AND c.PersonagemId = {personagemId}");

            var depois = await _contexto.Colecoes.AsNoTracking()
                .Where(c => c.UtilizadorId == utilizadorId && c.PersonagemId == personagemId)
                .Select(c => new { c.PontosAmizade, c.NivelAmizadeId })
                .FirstAsync();

            return new ResultadoPontos
            {
                PontosAmizade = depois.PontosAmizade,
                NivelAnterior = niveis.FirstOrDefault(n => n.Id == antes.NivelAmizadeId) ?? niveis.First(),
                NivelAtual = niveis.FirstOrDefault(n => n.Id == depois.NivelAmizadeId) ?? niveis.First()
            };
        }

        /// <summary>
        /// Desbloqueia as recompensas de todos os níveis entre o anterior
        /// (exclusive) e o atual (inclusive) e grava uma notificação por
        /// subida. INSERTs condicionais: correr duas vezes não duplica nada.
        /// Deve correr dentro da transação de quem chama.
        /// </summary>
        public async Task<List<RecompensaAmizade>> DesbloquearRecompensasAsync(
            int utilizadorId, int personagemId, string personagemNome, NivelAmizade nivelAnterior, NivelAmizade nivelAtual)
        {
            var recompensas = new List<RecompensaAmizade>();

            if (nivelAtual.Ordem <= nivelAnterior.Ordem)
            {
                return recompensas;
            }

            var niveisAtingidos = await _contexto.NiveisAmizade.AsNoTracking()
                .Where(n => n.Ordem > nivelAnterior.Ordem && n.Ordem <= nivelAtual.Ordem)
                .OrderBy(n => n.Ordem)
                .ToListAsync();

            foreach (var nivel in niveisAtingidos)
            {
                var ganhas = new List<RecompensaAmizade>();

                switch (nivel.Ordem)
                {
                    case NivelTitulo:
                    case NivelTituloUnico:
                    {
                        var titulo = await ObterOuCriarTituloAsync(personagemId, personagemNome, nivel);

                        int inseridas = await _contexto.Database.ExecuteSqlAsync(
                            $@"INSERT INTO TitulosUtilizador (UtilizadorId, TituloId, DataObtencao)
                               SELECT {utilizadorId}, {titulo.Id}, SYSUTCDATETIME()
                               WHERE NOT EXISTS (SELECT 1 FROM TitulosUtilizador
                                                 WHERE UtilizadorId = {utilizadorId} AND TituloId = {titulo.Id})");

                        if (inseridas == 1)
                        {
                            ganhas.Add(new RecompensaAmizade { Tipo = "Titulo", Id = titulo.Id, Nome = titulo.Nome });
                        }
                        break;
                    }

                    case NivelNotificacoes:
                        // Não há linha a inserir: a partir daqui a personagem
                        // manda mensagens (ver EnviarMensagensDiariasAsync).
                        ganhas.Add(new RecompensaAmizade { Tipo = "Notificacoes", Id = personagemId, Nome = "mensagens de " + personagemNome });
                        break;

                    case NivelEmblema:
                    {
                        var emblemas = await _contexto.Emblemas.AsNoTracking()
                            .Where(e => e.PersonagemId == personagemId)
                            .ToListAsync();

                        foreach (var emblema in emblemas)
                        {
                            int inseridas = await _contexto.Database.ExecuteSqlAsync(
                                $@"INSERT INTO EmblemasUtilizador (UtilizadorId, EmblemaId, DataObtencao, IsEquipado)
                                   SELECT {utilizadorId}, {emblema.Id}, SYSUTCDATETIME(), 0
                                   WHERE NOT EXISTS (SELECT 1 FROM EmblemasUtilizador
                                                     WHERE UtilizadorId = {utilizadorId} AND EmblemaId = {emblema.Id})");

                            if (inseridas == 1)
                            {
                                ganhas.Add(new RecompensaAmizade { Tipo = "Emblema", Id = emblema.Id, Nome = emblema.Nome });
                            }
                        }
                        break;
                    }

                    case NivelMoldura:
                    {
                        var molduras = await _contexto.MoldurasPerfil.AsNoTracking()
                            .Where(m => m.PersonagemId == personagemId)
                            .ToListAsync();

                        foreach (var moldura in molduras)
                        {
                            int inseridas = await _contexto.Database.ExecuteSqlAsync(
                                $@"INSERT INTO MoldurasUtilizador (UtilizadorId, MolduraId, DataObtencao)
                                   SELECT {utilizadorId}, {moldura.Id}, SYSUTCDATETIME()
                                   WHERE NOT EXISTS (SELECT 1 FROM MoldurasUtilizador
                                                     WHERE UtilizadorId = {utilizadorId} AND MolduraId = {moldura.Id})");

                            if (inseridas == 1)
                            {
                                ganhas.Add(new RecompensaAmizade { Tipo = "Moldura", Id = moldura.Id, Nome = moldura.Nome });
                            }
                        }
                        break;
                    }
                }

                // ----- Notificação da subida de nível -----
                string mensagem = "A tua amizade com " + personagemNome + " chegou a \"" + nivel.Nome + "\".";
                if (ganhas.Count > 0)
                {
                    mensagem += " Desbloqueaste: " + string.Join(", ", ganhas.Select(DescreverRecompensa)) + ".";
                }

                _contexto.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = utilizadorId,
                    Tipo = Notificacao.TipoAmizade,
                    Titulo = Truncar(personagemNome + " — " + nivel.Nome, 100),
                    Mensagem = Truncar(mensagem, 255),
                    IsLida = false,
                    DataCriacao = DateTime.UtcNow
                });

                recompensas.AddRange(ganhas);
            }

            await _contexto.SaveChangesAsync();

            return recompensas;
        }

        /// <summary>
        /// Notificações personalizadas (nível 4+): na primeira interação de
        /// cada dia, cada personagem que já seja "Confidente" ou melhor
        /// deixa uma mensagem ao utilizador (tabela Notificacoes, tipo
        /// "MensagemPersonagem"). O texto vem do conjunto "Diaria" da
        /// personagem (MensagensPersonagem, Migracao07) ao nível atual; se a
        /// personagem não tiver mensagens diárias, usa-se uma frase genérica.
        /// Devolve quantas mensagens foram enviadas.
        /// </summary>
        public async Task<int> EnviarMensagensDiariasAsync(int utilizadorId)
        {
            var nivelMinimo = await _contexto.NiveisAmizade.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Ordem == NivelNotificacoes);

            if (nivelMinimo == null)
            {
                return 0;
            }

            var confidentes = await _contexto.Colecoes.AsNoTracking()
                .Where(c => c.UtilizadorId == utilizadorId)
                .Join(_contexto.NiveisAmizade, c => c.NivelAmizadeId, n => n.Id,
                      (c, n) => new { c.PersonagemId, Nome = c.Personagem!.Nome, n.Ordem })
                .Where(x => x.Ordem >= NivelNotificacoes)
                .OrderByDescending(x => x.Ordem)
                .ToListAsync();

            // Uma mensagem "Diaria" por personagem, ao acaso entre as já desbloqueadas
            var proprias = await _mensagens.EscolherVariasAsync(
                confidentes.Select(x => (x.PersonagemId, x.Ordem)), MensagemPersonagem.TipoDiaria);

            foreach (var x in confidentes)
            {
                string nome = x.Nome ?? "Uma personagem";
                string texto = proprias.TryGetValue(x.PersonagemId, out var propria)
                    ? propria
                    : MensagemDiaria(nome, x.Ordem);

                _contexto.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = utilizadorId,
                    Tipo = Notificacao.TipoPersonagem,
                    Titulo = Truncar(nome, 100),
                    Mensagem = Truncar(texto, 255),
                    IsLida = false,
                    DataCriacao = DateTime.UtcNow
                });
            }

            if (confidentes.Count > 0)
            {
                await _contexto.SaveChangesAsync();
            }

            return confidentes.Count;
        }

        /// <summary>Mensagem do dia GENÉRICA, conforme o nível — só para personagens sem conjunto "Diaria" em MensagensPersonagem.</summary>
        public static string MensagemDiaria(string nome, int ordemNivel)
        {
            string[] frases = ordemNivel switch
            {
                >= NivelMoldura => new[]
                {
                    nome + ": \"Onde quer que estejas, estou contigo.\"",
                    nome + ": \"Já não sei o que era antes de te conhecer.\""
                },
                NivelTituloUnico => new[]
                {
                    nome + ": \"Sonhei contigo esta noite. Era um bom sonho.\"",
                    nome + ": \"Guardei-te um lugar ao meu lado.\""
                },
                NivelEmblema => new[]
                {
                    nome + ": \"Hoje é um bom dia para uma aventura, não achas?\"",
                    nome + ": \"Estava mesmo à tua espera.\""
                },
                _ => new[]
                {
                    nome + ": \"Bom dia! Dormiste bem?\"",
                    nome + ": \"Passa por cá quando puderes, tenho uma coisa para te contar.\""
                }
            };

            return frases[Random.Shared.Next(frases.Length)];
        }

        /// <summary>"título Melhor Amigo de Luna" / "emblema Rebento de Luna" / "moldura Moldura de Celeste" / "mensagens de Luna".</summary>
        public static string DescreverRecompensa(RecompensaAmizade r) => r.Tipo switch
        {
            "Emblema" => "emblema \"" + r.Nome + "\"",
            "Moldura" => "moldura \"" + r.Nome + "\"",
            "Notificacoes" => r.Nome,
            _ => "título \"" + r.Nome + "\""
        };

        /// <summary>
        /// Título da personagem para este nível (3 ou 6). Se a base de dados
        /// não tiver a linha (personagem criada depois da Migracao05), cria o
        /// título por omissão — nível 3 "Melhor Amigo de X"; nível 6 "Laço
        /// Especial com X" na cor da raridade.
        /// </summary>
        private async Task<Titulo> ObterOuCriarTituloAsync(int personagemId, string personagemNome, NivelAmizade nivel)
        {
            var existente = await _contexto.Titulos.AsNoTracking()
                .FirstOrDefaultAsync(t => t.PersonagemId == personagemId && t.NivelAmizadeId == nivel.Id);

            if (existente != null)
            {
                return existente;
            }

            bool unico = nivel.Ordem >= NivelTituloUnico;

            string? cor = unico
                ? await _contexto.Personagens.AsNoTracking()
                    .Where(p => p.Id == personagemId)
                    .Select(p => p.Raridade!.Cor)
                    .FirstOrDefaultAsync()
                : null;

            var novo = new Titulo
            {
                Nome = Truncar(unico ? "Laço Especial com " + personagemNome : "Melhor Amigo de " + personagemNome, 60),
                Descricao = Truncar(unico
                    ? "Um laço especial com " + personagemNome + "."
                    : "Chegaste a Melhor Amigo de " + personagemNome + ".", 255),
                PersonagemId = personagemId,
                NivelAmizadeId = nivel.Id,
                IsPersonalizado = unico,
                CorHex = cor
            };

            _contexto.Titulos.Add(novo);

            try
            {
                await _contexto.SaveChangesAsync();
                return novo;
            }
            catch (DbUpdateException)
            {
                // Dois pedidos criaram o mesmo título ao mesmo tempo: o índice
                // único deixou passar só um — usa-se esse.
                _contexto.Entry(novo).State = EntityState.Detached;

                return await _contexto.Titulos.AsNoTracking()
                    .FirstAsync(t => t.PersonagemId == personagemId && t.NivelAmizadeId == nivel.Id);
            }
        }

        private static string Truncar(string texto, int max) =>
            texto.Length <= max ? texto : texto.Substring(0, max - 1) + "…";
    }
}
