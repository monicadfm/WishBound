using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Services
{
    /// <summary>
    /// MENSAGENS DE PERSONAGEM — escolhe o que cada personagem diz a cada
    /// utilizador, conforme o nível de amizade entre os dois.
    ///
    /// Os conjuntos vivem na tabela MensagensPersonagem (Migracao07):
    /// para cada personagem, mensagens de três tipos ("Saudacao",
    /// "Aleatoria", "Diaria"), cada uma com o nível a partir do qual está
    /// desbloqueada.
    ///
    /// REGRA DE ESCOLHA: entre TODAS as mensagens do tipo pedido cujo nível
    /// já foi atingido, escolhe-se uma ao acaso. Quanto mais alta a amizade,
    /// mais falas há no conjunto — as novas juntam-se às antigas, não as
    /// substituem.
    ///
    /// Se a personagem não tiver mensagens desse tipo (personagem criada
    /// pelo administrador sem conjunto), quem chama usa uma frase genérica
    /// — ver os "fallbacks" no AmizadeController e no ServicoAmizade.
    /// </summary>
    public class ServicoMensagens
    {
        /// <summary>As mensagens diárias (notificações) só existem a partir deste nível — recompensa do nível 4.</summary>
        public const int NivelMensagensDiarias = ServicoAmizade.NivelNotificacoes;

        private readonly WishBoundContext _contexto;

        public ServicoMensagens(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>Nível mínimo a que uma mensagem deste tipo pode estar (as diárias só a partir do 4).</summary>
        public static int NivelMinimo(string tipo) =>
            tipo == MensagemPersonagem.TipoDiaria ? NivelMensagensDiarias : 1;

        /// <summary>
        /// Uma mensagem do tipo pedido para a personagem, ao nível de amizade
        /// indicado (ordem 1..7). null se não houver nenhuma desbloqueada.
        /// </summary>
        public async Task<string?> EscolherAsync(int personagemId, string tipo, int nivelOrdem)
        {
            var candidatas = await _contexto.MensagensPersonagem.AsNoTracking()
                .Where(m => m.PersonagemId == personagemId && m.TipoMensagem == tipo)
                .Join(_contexto.NiveisAmizade, m => m.NivelAmizadeId, n => n.Id,
                      (m, n) => new { m.Conteudo, n.Ordem })
                .Where(x => x.Ordem <= nivelOrdem)
                .ToListAsync();

            return Escolher(candidatas.Select(c => (c.Ordem, c.Conteudo)));
        }

        /// <summary>
        /// Uma mensagem por personagem, para várias personagens de uma vez
        /// (mensagens diárias). Devolve só as personagens que têm mensagem.
        /// </summary>
        public async Task<Dictionary<int, string>> EscolherVariasAsync(
            IEnumerable<(int PersonagemId, int NivelOrdem)> personagens, string tipo)
        {
            var lista = personagens.ToList();
            var resultado = new Dictionary<int, string>();

            if (lista.Count == 0)
            {
                return resultado;
            }

            var ids = lista.Select(p => p.PersonagemId).ToList();

            var todas = await _contexto.MensagensPersonagem.AsNoTracking()
                .Where(m => ids.Contains(m.PersonagemId) && m.TipoMensagem == tipo)
                .Join(_contexto.NiveisAmizade, m => m.NivelAmizadeId, n => n.Id,
                      (m, n) => new { m.PersonagemId, m.Conteudo, n.Ordem })
                .ToListAsync();

            foreach (var (personagemId, nivelOrdem) in lista)
            {
                var escolhida = Escolher(todas
                    .Where(m => m.PersonagemId == personagemId && m.Ordem <= nivelOrdem)
                    .Select(m => (m.Ordem, m.Conteudo)));

                if (escolhida != null)
                {
                    resultado[personagemId] = escolhida;
                }
            }

            return resultado;
        }

        /// <summary>
        /// O conjunto completo de uma personagem, com o estado para o
        /// utilizador (desbloqueada / alcançável pela raridade). O texto das
        /// mensagens bloqueadas não é enviado — só se sabe que existem.
        /// </summary>
        public async Task<List<MensagemResposta>> ObterConjuntoAsync(int personagemId, int nivelOrdem, int nivelMaximoOrdem)
        {
            var mensagens = await _contexto.MensagensPersonagem.AsNoTracking()
                .Where(m => m.PersonagemId == personagemId)
                .Join(_contexto.NiveisAmizade, m => m.NivelAmizadeId, n => n.Id,
                      (m, n) => new { m.Id, m.TipoMensagem, m.Conteudo, n.Ordem, NivelNome = n.Nome })
                .OrderBy(x => x.Ordem).ThenBy(x => x.Id)
                .ToListAsync();

            // Ordem de apresentação: saudações, reações, diárias
            return mensagens
                .OrderBy(m => OrdemTipo(m.TipoMensagem)).ThenBy(m => m.Ordem).ThenBy(m => m.Id)
                .Select(m =>
                {
                    bool desbloqueada = m.Ordem <= nivelOrdem;
                    return new MensagemResposta
                    {
                        Id = m.Id,
                        Tipo = m.TipoMensagem,
                        NivelOrdem = m.Ordem,
                        NivelNome = m.NivelNome,
                        Conteudo = desbloqueada ? m.Conteudo : null,
                        Desbloqueada = desbloqueada,
                        Alcancavel = m.Ordem <= nivelMaximoOrdem
                    };
                })
                .ToList();
        }

        /// <summary>Uma ao acaso entre todas as desbloqueadas.</summary>
        private static string? Escolher(IEnumerable<(int Ordem, string Conteudo)> candidatas)
        {
            var lista = candidatas.ToList();
            if (lista.Count == 0)
            {
                return null;
            }

            return lista[Random.Shared.Next(lista.Count)].Conteudo;
        }

        public static int OrdemTipo(string tipo) => tipo switch
        {
            MensagemPersonagem.TipoSaudacao => 1,
            MensagemPersonagem.TipoAleatoria => 2,
            _ => 3
        };

        /// <summary>"Saudação" / "Reação" / "Mensagem diária" — para o site.</summary>
        public static string NomeTipo(string tipo) => tipo switch
        {
            MensagemPersonagem.TipoSaudacao => "Saudação",
            MensagemPersonagem.TipoAleatoria => "Reação",
            MensagemPersonagem.TipoDiaria => "Mensagem diária",
            _ => tipo
        };
    }
}
