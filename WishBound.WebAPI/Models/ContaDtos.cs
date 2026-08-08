using System.ComponentModel.DataAnnotations;

namespace WishBound.WebAPI.Models
{
    // ============================================================
    //  DTOs (Data Transfer Objects) do sistema de contas.
    //  São as "formas" dos pedidos e respostas trocados entre o
    //  ClientAPI e a WebAPI - nunca expomos a entidade Utilizador
    //  completa (que contém o PasswordHash!).
    // ============================================================

    /// <summary>Pedido de registo de um novo utilizador.</summary>
    public class RegistoPedido
    {
        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome de utilizador deve ter entre 3 e 50 caracteres.")]
        public string NomeUtilizador { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email não é válido.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password é obrigatória.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A password deve ter pelo menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "A password deve incluir pelo menos 1 letra maiúscula, 1 número e 1 símbolo (ex.: ! ou ?).")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Pedido de login (aceita nome de utilizador OU email).</summary>
    public class LoginPedido
    {
        [Required(ErrorMessage = "Indique o nome de utilizador ou o email.")]
        public string Identificador { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password é obrigatória.")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Pedido de login com Google. O ClientAPI só envia este pedido DEPOIS
    /// de a Google confirmar a identidade do utilizador (OAuth 2.0), por
    /// isso os dados chegam já verificados pela Google.
    /// </summary>
    public class LoginGooglePedido
    {
        [Required(ErrorMessage = "O identificador Google é obrigatório.")]
        [StringLength(100)]
        public string GoogleId { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email não é válido.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        // Nome apresentado na conta Google (usado para sugerir o nome de utilizador)
        [StringLength(100)]
        public string? Nome { get; set; }

        // Fotografia de perfil da conta Google (opcional)
        [StringLength(255)]
        public string? FotoUrl { get; set; }
    }

    /// <summary>Pedido de validação de email (token recebido "por email").</summary>
    public class ValidarEmailPedido
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>Pedido de reenvio do email de validação / recuperação de password.</summary>
    public class EmailPedido
    {
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email não é válido.")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Pedido de reposição de password com um token de recuperação.</summary>
    public class ReporPasswordPedido
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "A nova password é obrigatória.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A password deve ter pelo menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "A password deve incluir pelo menos 1 letra maiúscula, 1 número e 1 símbolo (ex.: ! ou ?).")]
        public string NovaPassword { get; set; } = string.Empty;
    }

    /// <summary>Pedido de alteração de password (utilizador autenticado).</summary>
    public class AlterarPasswordPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        [Required(ErrorMessage = "A password atual é obrigatória.")]
        public string PasswordAtual { get; set; } = string.Empty;

        [Required(ErrorMessage = "A nova password é obrigatória.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A password deve ter pelo menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "A password deve incluir pelo menos 1 letra maiúscula, 1 número e 1 símbolo (ex.: ! ou ?).")]
        public string NovaPassword { get; set; } = string.Empty;
    }

    /// <summary>Pedido de atualização do perfil.</summary>
    public class PerfilPedido
    {
        [Required]
        public int UtilizadorId { get; set; }

        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome de utilizador deve ter entre 3 e 50 caracteres.")]
        public string NomeUtilizador { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "O caminho da foto não pode ter mais de 255 caracteres.")]
        public string? FotoPerfilUrl { get; set; }
    }

    /// <summary>
    /// Resposta com os dados públicos de um utilizador
    /// (o que o ClientAPI precisa para criar a sessão).
    /// </summary>
    public class UtilizadorResposta
    {
        public int Id { get; set; }
        public string NomeUtilizador { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailValidado { get; set; }
        public bool IsAdmin { get; set; }
        public string? FotoPerfilUrl { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? UltimoLogin { get; set; }
    }

    /// <summary>
    /// Resposta das operações que geram um token (registo, reenvio de
    /// validação, recuperação de password).
    ///
    /// NOTA (modo de desenvolvimento): como ainda não há envio real de
    /// emails, o token é devolvido na resposta para o ClientAPI mostrar
    /// o link no ecrã. Quando existir um serviço de email (SMTP), o token
    /// passa a ser enviado por email e removido desta resposta.
    /// </summary>
    public class TokenResposta
    {
        public string Mensagem { get; set; } = string.Empty;
        public string? Token { get; set; }
    }
}
