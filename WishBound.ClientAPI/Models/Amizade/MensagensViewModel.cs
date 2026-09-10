namespace WishBound.ClientAPI.Models.Amizade
{
    // ============================================================
    //  MENSAGENS DE PERSONAGEM — espelhos dos DTOs de api/mensagens
    //  e api/admin/mensagens.
    // ============================================================

    /// <summary>Uma mensagem do conjunto de uma personagem (o texto só vem se estiver desbloqueada).</summary>
    public class Mensagem
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public string NivelNome { get; set; } = string.Empty;
        public string? Conteudo { get; set; }
        public bool Desbloqueada { get; set; }
        public bool Alcancavel { get; set; }

        public string TipoNome => NomeTipo(Tipo);

        public static string NomeTipo(string tipo) => tipo switch
        {
            "Saudacao" => "Saudação",
            "Aleatoria" => "Reação",
            "Diaria" => "Mensagem diária",
            _ => tipo
        };

        /// <summary>Explicação curta de cada tipo, para a página de detalhes e a gestão.</summary>
        public static string DescricaoTipo(string tipo) => tipo switch
        {
            "Saudacao" => "quando abres esta página ou quando é a tua companheira na página inicial",
            "Aleatoria" => "depois de cada interação",
            "Diaria" => "deixada nas notificações na primeira interação de cada dia (a partir do nível 4)",
            _ => ""
        };
    }

    /// <summary>Conjunto de mensagens de uma personagem da coleção, com a saudação de hoje.</summary>
    public class MensagensPersonagem
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public int NivelMaximoOrdem { get; set; }
        public string? Saudacao { get; set; }
        public bool EhCompanheira { get; set; }
        public int NivelMensagensDiarias { get; set; } = 4;
        public int Total { get; set; }
        public int Desbloqueadas { get; set; }
        public List<Mensagem> Mensagens { get; set; } = new List<Mensagem>();

        /// <summary>Os tipos pela ordem de apresentação, só os que existem.</summary>
        public IEnumerable<IGrouping<string, Mensagem>> PorTipo =>
            Mensagens.GroupBy(m => m.Tipo);
    }

    /// <summary>Personagem da coleção que pode ser a companheira da página inicial.</summary>
    public class Companheira
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public string NivelAmizadeNome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }

        public string Cor => string.IsNullOrEmpty(RaridadeCor) ? "#9aa5b1" : RaridadeCor;
        public string Imagem => string.IsNullOrEmpty(ImagemUrl) ? "/img/personagens/desconhecido.svg" : ImagemUrl;
    }

    /// <summary>A companheira escolhida (ou nenhuma), a saudação dela e as alternativas.</summary>
    public class CompanheiraViewModel
    {
        public Companheira? Escolhida { get; set; }
        public string? Saudacao { get; set; }
        public List<Companheira> Candidatas { get; set; } = new List<Companheira>();
    }

    // ----- Gestão -----

    public class MensagemAdmin
    {
        public int Id { get; set; }
        public int PersonagemId { get; set; }
        public string PersonagemNome { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public string NivelNome { get; set; } = string.Empty;
        public string Conteudo { get; set; } = string.Empty;
        public bool Alcancavel { get; set; }

        public string TipoNome => Mensagem.NomeTipo(Tipo);
    }

    public class PersonagemMensagensResumo
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
        public int Total { get; set; }

        public string Cor => string.IsNullOrEmpty(RaridadeCor) ? "#9aa5b1" : RaridadeCor;
        public string Imagem => string.IsNullOrEmpty(ImagemUrl) ? "/img/personagens/desconhecido.svg" : ImagemUrl;
    }

    /// <summary>Página Gestão → Mensagens: resumo por personagem + a lista (de todas ou de uma).</summary>
    public class GestaoMensagensViewModel
    {
        public List<PersonagemMensagensResumo> Personagens { get; set; } = new List<PersonagemMensagensResumo>();
        public List<MensagemAdmin> Mensagens { get; set; } = new List<MensagemAdmin>();
        public List<NivelAmizade> Niveis { get; set; } = new List<NivelAmizade>();
        public int NivelMinimoDiaria { get; set; } = 4;

        /// <summary>Personagem filtrada (null = todas).</summary>
        public int? PersonagemId { get; set; }

        public PersonagemMensagensResumo? PersonagemAtual =>
            PersonagemId.HasValue ? Personagens.FirstOrDefault(p => p.PersonagemId == PersonagemId.Value) : null;

        /// <summary>Formulário de criação/edição (vazio na lista; preenchido ao editar).</summary>
        public MensagemFormViewModel Form { get; set; } = new MensagemFormViewModel();
    }

    /// <summary>Formulário de uma mensagem (criar ou editar).</summary>
    public class MensagemFormViewModel
    {
        public int Id { get; set; }
        public int PersonagemId { get; set; }
        public string PersonagemNome { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Saudacao";
        public int NivelOrdem { get; set; } = 1;
        public string Conteudo { get; set; } = string.Empty;
        public string? Motivo { get; set; }
    }
}
