using System.ComponentModel.DataAnnotations;

namespace WishBound.ClientAPI.Models.Gestao
{
    // ============================================================
    //  Modelos da ADMINISTRAÇÃO DE CONTAS (espelhos dos DTOs de api/admin)
    //  + os formulários das ações do painel.
    // ============================================================

    /// <summary>Linha da lista de contas.</summary>
    public class ContaResumo
    {
        public int Id { get; set; }
        public string NomeUtilizador { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailValidado { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsAtivo { get; set; }
        public bool TemPasswordLocal { get; set; }
        public bool ContaGoogle { get; set; }
        public string? FotoPerfilUrl { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public decimal SaldoMoedas { get; set; }
        public decimal SaldoBilhetes { get; set; }
        public int PersonagensDistintas { get; set; }
        public int TotalCopias { get; set; }
    }

    /// <summary>Página da lista de contas.</summary>
    public class ListaContasViewModel
    {
        public List<ContaResumo> Itens { get; set; } = new List<ContaResumo>();
        public int Total { get; set; }
        public int Pagina { get; set; } = 1;
        public int Tamanho { get; set; } = 20;
        public int TotalPaginas { get; set; } = 1;
        public int TotalContas { get; set; }
        public int TotalAtivas { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalPorValidar { get; set; }

        // Estado dos filtros (preenchido pelo controller, não vem da API)
        public string? Pesquisa { get; set; }
        public string Filtro { get; set; } = "todos";
    }

    public class ContaCarteira
    {
        public int TipoMoedaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Saldo { get; set; }
    }

    public class ContaItemColecao
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public int RaridadeOrdem { get; set; }
        public int Quantidade { get; set; }
        public bool IsFavorito { get; set; }
        public int PontosAmizade { get; set; }
        public string NivelAmizadeNome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public int NivelMaximoOrdem { get; set; }
        public int PontosNivelMaximo { get; set; }
        public DateTime DataObtencao { get; set; }

        public int PercentagemAmizade => PontosNivelMaximo <= 0
            ? 0
            : (int)Math.Clamp(Math.Round(100.0 * PontosAmizade / PontosNivelMaximo), 0, 100);
    }

    public class ContaPersonagemOpcao
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string RaridadeNome { get; set; } = string.Empty;
        public bool IsAtivo { get; set; }
    }

    public class ContaRecompensa
    {
        public string Tipo { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? PersonagemNome { get; set; }
        public string? NivelNome { get; set; }
        public int NivelOrdem { get; set; }
        public string? CorHex { get; set; }
        public bool Obtida { get; set; }
        public bool Equipada { get; set; }
        public DateTime? DataObtencao { get; set; }

        public string TipoTexto => Tipo switch
        {
            "Emblema" => "Emblema",
            "Moldura" => "Moldura",
            _ => "Título"
        };
    }

    public class ContaTransacao
    {
        public int Id { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Montante { get; set; }
        public string TipoTransacao { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
        public DateTime Data { get; set; }

        public bool Ganho => TipoTransacao == "Ganho";
    }

    /// <summary>Uma linha do registo de ações de administração.</summary>
    public class AcaoAdmin
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string AdminNome { get; set; } = string.Empty;
        public int? UtilizadorAlvoId { get; set; }
        public string? UtilizadorAlvoNome { get; set; }
        public string Acao { get; set; } = string.Empty;
        public string? TabelaAlvo { get; set; }
        public int? RegistoAlvoId { get; set; }
        public string? Detalhes { get; set; }
        public DateTime Data { get; set; }

        /// <summary>"Moeda", "Estado", ... (o que vem antes dos dois pontos).</summary>
        public string Categoria
        {
            get
            {
                int i = Acao.IndexOf(':');
                return i > 0 ? Acao.Substring(0, i) : Acao;
            }
        }
    }

    /// <summary>Página de uma conta (detalhe + formulários).</summary>
    public class ContaDetalheViewModel
    {
        public ContaResumo Conta { get; set; } = new ContaResumo();
        public List<ContaCarteira> Carteiras { get; set; } = new List<ContaCarteira>();
        public int CapacidadeBase { get; set; }
        public int CapacidadeExtra { get; set; }
        public int Ocupado { get; set; }
        public int InteracoesRestantes { get; set; }
        public int DiaRecompensaDiaria { get; set; }
        public DateOnly? UltimoLoginDiario { get; set; }
        public string? TituloAtual { get; set; }
        public string? MolduraAtual { get; set; }
        public List<ContaItemColecao> Colecao { get; set; } = new List<ContaItemColecao>();
        public List<ContaPersonagemOpcao> PersonagensDisponiveis { get; set; } = new List<ContaPersonagemOpcao>();
        public List<ContaRecompensa> Recompensas { get; set; } = new List<ContaRecompensa>();
        public List<ContaTransacao> UltimasTransacoes { get; set; } = new List<ContaTransacao>();
        public List<AcaoAdmin> UltimasAcoes { get; set; } = new List<AcaoAdmin>();

        public int Capacidade => CapacidadeBase + CapacidadeExtra;
        public int Livres => Math.Max(0, Capacidade - Ocupado);

        /// <summary>true quando a conta que está a ver é a do próprio administrador.</summary>
        public bool EhAPropriaConta { get; set; }

        public IEnumerable<ContaRecompensa> Titulos => Recompensas.Where(r => r.Tipo == "Titulo");
        public IEnumerable<ContaRecompensa> Emblemas => Recompensas.Where(r => r.Tipo == "Emblema");
        public IEnumerable<ContaRecompensa> Molduras => Recompensas.Where(r => r.Tipo == "Moldura");
    }

    /// <summary>Resposta comum das ações (espelho de AdminAcaoResultado).</summary>
    public class ResultadoAcaoAdmin
    {
        public string Mensagem { get; set; } = string.Empty;
        public decimal? NovoValor { get; set; }
    }

    // ------------------------------------------------------------
    //  Formulários (POST do painel)
    // ------------------------------------------------------------

    public class ReporPasswordAdminViewModel
    {
        [Required(ErrorMessage = "A nova password é obrigatória.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A password deve ter pelo menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "A password deve incluir pelo menos 1 letra maiúscula, 1 número e 1 símbolo (ex.: ! ou ?).")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova password")]
        public string NovaPassword { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Motivo")]
        public string? Motivo { get; set; }
    }
}
