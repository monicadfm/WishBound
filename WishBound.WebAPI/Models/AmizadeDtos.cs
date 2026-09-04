using System.ComponentModel.DataAnnotations;

namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs do sistema de amizade (o que a API recebe e devolve).
    // ============================================================

    /// <summary>Um nível de amizade e o que desbloqueia.</summary>
    public class NivelAmizadeResposta
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int PontosNecessarios { get; set; }
        public int Ordem { get; set; }

        /// <summary>O que este nível desbloqueia (vazio nos níveis 1 e 2).</summary>
        public string Recompensa { get; set; } = string.Empty;
    }

    /// <summary>Emblema ("rebento") — com a indicação de se já foi ganho e se está no perfil.</summary>
    public class EmblemaResposta
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemUrl { get; set; }
        public int? PersonagemId { get; set; }
        public string? PersonagemNome { get; set; }
        public string? NivelNome { get; set; }
        public bool Obtido { get; set; }
        public bool Equipado { get; set; }
        public DateTime? DataObtencao { get; set; }
    }

    /// <summary>Título — com a indicação de se já foi ganho e se está equipado.</summary>
    public class TituloResposta
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int? PersonagemId { get; set; }
        public string? PersonagemNome { get; set; }
        public string? NivelNome { get; set; }
        public int NivelOrdem { get; set; }
        public bool IsPersonalizado { get; set; }

        /// <summary>Cor do título (só os títulos únicos de nível 6 têm).</summary>
        public string? CorHex { get; set; }
        public bool Obtido { get; set; }
        public bool Equipado { get; set; }
        public DateTime? DataObtencao { get; set; }
    }

    /// <summary>Moldura de perfil — com a indicação de se já foi ganha e se está equipada.</summary>
    public class MolduraResposta
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string? CorHex { get; set; }
        public int? PersonagemId { get; set; }
        public string? PersonagemNome { get; set; }
        public string? NivelNome { get; set; }
        public bool Obtida { get; set; }
        public bool Equipada { get; set; }
        public DateTime? DataObtencao { get; set; }
    }

    /// <summary>Estado da amizade com uma personagem da coleção.</summary>
    public class AmizadePersonagemResposta
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public int RaridadeOrdem { get; set; }

        public int PontosAmizade { get; set; }
        public int NivelAmizadeId { get; set; }
        public string NivelAmizadeNome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }

        /// <summary>Nível mais alto que esta personagem pode atingir (Comum 3 ... Mítico 7).</summary>
        public int NivelMaximoOrdem { get; set; }
        public string NivelMaximoNome { get; set; } = string.Empty;

        /// <summary>Pontos do nível atual (para a barra de progresso).</summary>
        public int PontosNivelAtual { get; set; }

        /// <summary>Pontos do próximo nível (null quando já está no máximo da raridade).</summary>
        public int? PontosProximoNivel { get; set; }

        public DateOnly? UltimaInteracao { get; set; }
    }

    /// <summary>Um nível, visto da amizade com UMA personagem (para a lista da página de detalhes).</summary>
    public class NivelPersonagemResposta
    {
        public int Ordem { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int PontosNecessarios { get; set; }
        public string Recompensa { get; set; } = string.Empty;

        /// <summary>Já chegou a este nível com esta personagem.</summary>
        public bool Atingido { get; set; }

        /// <summary>A raridade da personagem permite chegar a este nível.</summary>
        public bool Alcancavel { get; set; }
    }

    /// <summary>
    /// Detalhe da amizade com uma personagem: progresso, os níveis com o
    /// que cada um desbloqueia e as recompensas desta personagem (título,
    /// título único, emblema, moldura) com o estado — ganho? equipado?
    /// </summary>
    public class AmizadePersonagemDetalhe : AmizadePersonagemResposta
    {
        public int InteracoesRestantes { get; set; }
        public int InteracoesPorDia { get; set; }
        public int PontosPorInteracao { get; set; }

        public List<NivelPersonagemResposta> Niveis { get; set; } = new List<NivelPersonagemResposta>();

        /// <summary>Título de nível 3 ("Melhor Amigo de X") — null se ainda não existir na BD.</summary>
        public TituloResposta? Titulo { get; set; }

        /// <summary>Título único de nível 6 (só Lendário/Mítico).</summary>
        public TituloResposta? TituloUnico { get; set; }

        /// <summary>Emblema de nível 5 (só Épico ou melhor).</summary>
        public EmblemaResposta? Emblema { get; set; }

        /// <summary>Moldura de nível 7 (só Mítico).</summary>
        public MolduraResposta? Moldura { get; set; }

        /// <summary>A personagem já manda mensagens (nível 4 ou mais).</summary>
        public bool NotificacoesAtivas { get; set; }

        /// <summary>Emblemas que o utilizador já tem equipados (para o limite de 3).</summary>
        public int EmblemasEquipados { get; set; }
        public int MaximoEmblemasEquipados { get; set; }
    }

    /// <summary>
    /// Resumo da amizade para o perfil e para a coleção: interações de
    /// hoje, regras, níveis e as recompensas já ganhas (títulos, emblemas,
    /// molduras) com o que está equipado.
    /// </summary>
    public class AmizadeResposta
    {
        public int InteracoesRestantes { get; set; }
        public int InteracoesPorDia { get; set; }
        public int PontosPorInteracao { get; set; }

        /// <summary>Pontos por cópia repetida obtida, por ordem de raridade (1..5).</summary>
        public Dictionary<int, int> PontosPorRepetida { get; set; } = new Dictionary<int, int>();

        /// <summary>Multiplicador das repetidas obtidas num banner de evento.</summary>
        public int MultiplicadorEvento { get; set; }

        public int MaximoEmblemasEquipados { get; set; }

        public List<NivelAmizadeResposta> Niveis { get; set; } = new List<NivelAmizadeResposta>();

        /// <summary>Só os já ganhos.</summary>
        public List<TituloResposta> Titulos { get; set; } = new List<TituloResposta>();
        public List<EmblemaResposta> Emblemas { get; set; } = new List<EmblemaResposta>();
        public List<MolduraResposta> Molduras { get; set; } = new List<MolduraResposta>();

        public int? TituloAtualId { get; set; }
        public string? TituloAtualNome { get; set; }
        public string? TituloAtualCor { get; set; }
        public int? MolduraAtualId { get; set; }
        public string? MolduraAtualCor { get; set; }
        public string? MolduraAtualNome { get; set; }
    }

    /// <summary>Recompensa desbloqueada numa subida de nível.</summary>
    public class RecompensaAmizade
    {
        /// <summary>"Titulo", "Emblema", "Moldura" ou "Notificacoes".</summary>
        public string Tipo { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    /// <summary>Resultado de uma interação com uma personagem.</summary>
    public class InteracaoResposta
    {
        public string Mensagem { get; set; } = string.Empty;

        /// <summary>O que a personagem "diz" (saudação simples por nível).</summary>
        public string Reacao { get; set; } = string.Empty;

        public int PersonagemId { get; set; }
        public string PersonagemNome { get; set; } = string.Empty;
        public int PontosGanhos { get; set; }
        public int PontosAmizade { get; set; }
        public string NivelAnterior { get; set; } = string.Empty;
        public string NivelAtual { get; set; } = string.Empty;
        public bool SubiuDeNivel { get; set; }
        public bool NivelMaximo { get; set; }
        public int InteracoesRestantes { get; set; }
        public List<RecompensaAmizade> Recompensas { get; set; } = new List<RecompensaAmizade>();

        /// <summary>Mensagens diárias de personagens (nível 4+) deixadas nesta primeira interação do dia.</summary>
        public int MensagensDiarias { get; set; }
    }

    /// <summary>Interagir com uma personagem da coleção (gasta 1 das 3 interações do dia).</summary>
    public class InteragirPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        [Required]
        public int PersonagemId { get; set; }
    }

    /// <summary>Escolher o título do perfil (null = sem título).</summary>
    public class EquiparTituloPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        public int? TituloId { get; set; }
    }

    /// <summary>Escolher a moldura do perfil (null = sem moldura).</summary>
    public class EquiparMolduraPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        public int? MolduraId { get; set; }
    }

    /// <summary>Pôr ou tirar um emblema do perfil (máximo 3 equipados).</summary>
    public class EquiparEmblemaPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        [Required]
        public int EmblemaId { get; set; }

        public bool Equipar { get; set; } = true;
    }
}
