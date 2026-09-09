using System.ComponentModel.DataAnnotations;

namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs da ADMINISTRAÇÃO DE CONTAS (api/admin).
    //
    //  Todos os pedidos de escrita trazem o AdminId (quem está a fazer a
    //  ação) e um Motivo opcional, que fica no registo de ações. A API
    //  confirma que o AdminId é mesmo uma conta de administrador ativa —
    //  é a mesma fronteira de confiança do resto da API (chave partilhada
    //  + Id enviado pelo site).
    // ============================================================

    // ------------------------------------------------------------
    //  Pedidos
    // ------------------------------------------------------------

    /// <summary>Base de todos os pedidos de administração.</summary>
    public abstract class AdminPedido
    {
        [Required]
        public int AdminId { get; set; }

        [StringLength(255, ErrorMessage = "O motivo não pode ter mais de 255 caracteres.")]
        public string? Motivo { get; set; }
    }

    /// <summary>
    /// Altera o estado de uma conta. Só os campos preenchidos (não null)
    /// são alterados — dá para mudar uma coisa de cada vez.
    /// </summary>
    public class AdminEstadoPedido : AdminPedido
    {
        public bool? IsAtivo { get; set; }
        public bool? IsAdmin { get; set; }
        public bool? EmailValidado { get; set; }
    }

    /// <summary>Define uma nova password para a conta (sem precisar da antiga).</summary>
    public class AdminReporPasswordPedido : AdminPedido
    {
        [Required(ErrorMessage = "A nova password é obrigatória.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A password deve ter pelo menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "A password deve incluir pelo menos 1 letra maiúscula, 1 número e 1 símbolo (ex.: ! ou ?).")]
        public string NovaPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dá ou tira moeda. Quantidade positiva = crédito ("Ganho");
    /// negativa = débito ("Gasto"). Um débito nunca deixa o saldo negativo:
    /// tira no máximo o que houver e a resposta diz quanto saiu de facto.
    /// </summary>
    public class AdminMoedaPedido : AdminPedido
    {
        [Required]
        public int TipoMoedaId { get; set; }

        [Required]
        [Range(-1000000000, 1000000000, ErrorMessage = "Quantidade fora dos limites.")]
        public decimal Quantidade { get; set; }
    }

    /// <summary>
    /// Dá ou tira cópias de uma personagem. Quantidade positiva = acrescenta
    /// (a primeira cópia cria a linha na coleção); negativa = retira (se a
    /// contagem chegar a zero a linha é apagada e a amizade com essa
    /// personagem perde-se — as recompensas já ganhas ficam).
    /// </summary>
    public class AdminPersonagemPedido : AdminPedido
    {
        [Required]
        public int PersonagemId { get; set; }

        [Required]
        [Range(-1000, 1000, ErrorMessage = "A quantidade tem de estar entre -1000 e 1000.")]
        public int Quantidade { get; set; }
    }

    /// <summary>Define a capacidade extra do inventário (lugares além dos 100 base).</summary>
    public class AdminInventarioPedido : AdminPedido
    {
        [Required]
        [Range(0, 1000000, ErrorMessage = "A capacidade extra tem de estar entre 0 e 1.000.000.")]
        public int CapacidadeExtra { get; set; }
    }

    /// <summary>
    /// Define os pontos de amizade com uma personagem da coleção. O valor é
    /// limitado ao máximo da raridade e o nível é recalculado; se subir,
    /// as recompensas desses níveis são desbloqueadas como numa interação.
    /// </summary>
    public class AdminAmizadePedido : AdminPedido
    {
        [Required]
        public int PersonagemId { get; set; }

        [Required]
        [Range(0, 100000, ErrorMessage = "Os pontos têm de estar entre 0 e 100.000.")]
        public int Pontos { get; set; }
    }

    /// <summary>
    /// Concede ou revoga uma recompensa de amizade diretamente:
    /// Tipo = "Titulo" | "Emblema" | "Moldura"; Conceder = true dá,
    /// false tira (e desequipa do perfil se estiver em uso).
    /// </summary>
    public class AdminRecompensaPedido : AdminPedido
    {
        [Required]
        [RegularExpression("^(Titulo|Emblema|Moldura)$", ErrorMessage = "Tipo inválido (Titulo, Emblema ou Moldura).")]
        public string Tipo { get; set; } = "Titulo";

        [Required]
        public int Id { get; set; }

        public bool Conceder { get; set; } = true;
    }

    // ------------------------------------------------------------
    //  Respostas
    // ------------------------------------------------------------

    /// <summary>Linha da lista de contas.</summary>
    public class AdminUtilizadorResumo
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

    /// <summary>Lista paginada de contas.</summary>
    public class AdminListaUtilizadores
    {
        public List<AdminUtilizadorResumo> Itens { get; set; } = new List<AdminUtilizadorResumo>();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int Tamanho { get; set; }
        public int TotalPaginas => Tamanho <= 0 ? 1 : (int)Math.Ceiling(Total / (double)Tamanho);

        // Contadores para o topo do painel
        public int TotalContas { get; set; }
        public int TotalAtivas { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalPorValidar { get; set; }
    }

    /// <summary>Saldo numa moeda (com o nome).</summary>
    public class AdminCarteira
    {
        public int TipoMoedaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Saldo { get; set; }
    }

    /// <summary>Uma personagem da coleção da conta, com a amizade.</summary>
    public class AdminItemColecao
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
    }

    /// <summary>Personagem que a conta ainda NÃO tem (para a dropdown de "dar personagem").</summary>
    public class AdminPersonagemOpcao
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string RaridadeNome { get; set; } = string.Empty;
        public bool IsAtivo { get; set; }
    }

    /// <summary>Uma recompensa de amizade (título, emblema ou moldura) e se a conta a tem.</summary>
    public class AdminRecompensa
    {
        public string Tipo { get; set; } = string.Empty;   // Titulo | Emblema | Moldura
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? PersonagemNome { get; set; }
        public string? NivelNome { get; set; }
        public int NivelOrdem { get; set; }
        public string? CorHex { get; set; }
        public bool Obtida { get; set; }
        public bool Equipada { get; set; }
        public DateTime? DataObtencao { get; set; }
    }

    /// <summary>Um movimento de moeda (para o detalhe da conta).</summary>
    public class AdminTransacao
    {
        public int Id { get; set; }
        public string MoedaNome { get; set; } = string.Empty;
        public decimal Montante { get; set; }
        public string TipoTransacao { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
        public DateTime Data { get; set; }
    }

    /// <summary>Uma linha do registo de ações.</summary>
    public class AdminAcaoResposta
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
    }

    /// <summary>Tudo o que a página de uma conta mostra.</summary>
    public class AdminUtilizadorDetalhe
    {
        public AdminUtilizadorResumo Conta { get; set; } = new AdminUtilizadorResumo();

        public List<AdminCarteira> Carteiras { get; set; } = new List<AdminCarteira>();

        public int CapacidadeBase { get; set; }
        public int CapacidadeExtra { get; set; }
        public int Ocupado { get; set; }

        public int InteracoesRestantes { get; set; }
        public int DiaRecompensaDiaria { get; set; }
        public DateOnly? UltimoLoginDiario { get; set; }

        public string? TituloAtual { get; set; }
        public string? MolduraAtual { get; set; }

        public List<AdminItemColecao> Colecao { get; set; } = new List<AdminItemColecao>();
        public List<AdminPersonagemOpcao> PersonagensDisponiveis { get; set; } = new List<AdminPersonagemOpcao>();
        public List<AdminRecompensa> Recompensas { get; set; } = new List<AdminRecompensa>();
        public List<AdminTransacao> UltimasTransacoes { get; set; } = new List<AdminTransacao>();
        public List<AdminAcaoResposta> UltimasAcoes { get; set; } = new List<AdminAcaoResposta>();
    }

    /// <summary>Resposta comum das ações de escrita.</summary>
    public class AdminAcaoResultado
    {
        public string Mensagem { get; set; } = string.Empty;

        /// <summary>Saldo, contagem de cópias, pontos... conforme a ação.</summary>
        public decimal? NovoValor { get; set; }
    }
}
