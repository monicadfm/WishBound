using System.ComponentModel.DataAnnotations;

namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs das MENSAGENS DE PERSONAGEM (api/mensagens e api/admin/mensagens)
    // ============================================================

    /// <summary>Uma mensagem do conjunto de uma personagem, com o estado para este utilizador.</summary>
    public class MensagemResposta
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public string NivelNome { get; set; } = string.Empty;

        /// <summary>Texto da mensagem — só vem quando está desbloqueada.</summary>
        public string? Conteudo { get; set; }

        /// <summary>O utilizador já atingiu o nível desta mensagem com a personagem.</summary>
        public bool Desbloqueada { get; set; }

        /// <summary>A raridade da personagem chega ao nível desta mensagem.</summary>
        public bool Alcancavel { get; set; }
    }

    /// <summary>Conjunto de mensagens de UMA personagem da coleção (página de detalhes).</summary>
    public class MensagensPersonagemResposta
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public int NivelMaximoOrdem { get; set; }

        /// <summary>Saudação escolhida ao acaso para esta visita (null se a personagem não tiver saudações).</summary>
        public string? Saudacao { get; set; }

        /// <summary>Esta personagem é a companheira que recebe o utilizador na página inicial.</summary>
        public bool EhCompanheira { get; set; }

        /// <summary>Nível a partir do qual as mensagens diárias (notificações) são enviadas.</summary>
        public int NivelMensagensDiarias { get; set; }

        public int Total { get; set; }
        public int Desbloqueadas { get; set; }

        public List<MensagemResposta> Mensagens { get; set; } = new List<MensagemResposta>();
    }

    /// <summary>Uma personagem da coleção que pode ser escolhida como companheira.</summary>
    public class CandidataCompanheira
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public string NivelAmizadeNome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
    }

    /// <summary>A companheira da página inicial (a escolhida + a saudação de hoje) e as alternativas.</summary>
    public class CompanheiraResposta
    {
        /// <summary>null quando o utilizador ainda não escolheu nenhuma.</summary>
        public CandidataCompanheira? Escolhida { get; set; }

        public string? Saudacao { get; set; }

        /// <summary>Todas as personagens da coleção, para o utilizador escolher (ou trocar).</summary>
        public List<CandidataCompanheira> Candidatas { get; set; } = new List<CandidataCompanheira>();
    }

    public class EscolherCompanheiraPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        /// <summary>null = deixar de ter companheira.</summary>
        public int? PersonagemId { get; set; }
    }

    // ----- Administração (api/admin/mensagens) -----

    public class AdminMensagemResposta
    {
        public int Id { get; set; }
        public int PersonagemId { get; set; }
        public string PersonagemNome { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public string NivelNome { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;

        /// <summary>A raridade da personagem chega a este nível (senão a mensagem nunca será vista).</summary>
        public bool Alcancavel { get; set; }
    }

    /// <summary>Resumo por personagem para a página de gestão (quantas mensagens de cada tipo).</summary>
    public class AdminPersonagemMensagens
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public int NivelMaximoOrdem { get; set; }
        public int Saudacoes { get; set; }
        public int Aleatorias { get; set; }
        public int Diarias { get; set; }
        public int Total => Saudacoes + Aleatorias + Diarias;
    }

    public class AdminListaMensagens
    {
        public List<AdminPersonagemMensagens> Personagens { get; set; } = new List<AdminPersonagemMensagens>();
        public List<AdminMensagemResposta> Mensagens { get; set; } = new List<AdminMensagemResposta>();
        public List<NivelAmizadeResposta> Niveis { get; set; } = new List<NivelAmizadeResposta>();
        public int NivelMinimoDiaria { get; set; }
    }

    public class AdminMensagemPedido
    {
        [Required]
        public int AdminId { get; set; }

        [Required]
        public int PersonagemId { get; set; }

        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Ordem do nível (1..7) a partir do qual a mensagem fica disponível.</summary>
        [Range(1, 7)]
        public int NivelOrdem { get; set; } = 1;

        [Required(ErrorMessage = "A mensagem não pode estar vazia.")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "A mensagem deve ter entre 2 e 500 caracteres.")]
        public string Conteudo { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Motivo { get; set; }
    }
}
