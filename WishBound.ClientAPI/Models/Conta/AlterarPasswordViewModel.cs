using System.ComponentModel.DataAnnotations;

namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>
    /// Formulário de alteração de password (utilizador autenticado).
    ///
    /// A mesma página serve dois casos:
    ///   - ALTERAR: conta normal — é preciso confirmar a password atual;
    ///   - DEFINIR: conta criada com Google, que ainda não tem password local
    ///     (nesse caso não há password atual para confirmar).
    /// </summary>
    public class AlterarPasswordViewModel
    {
        /// <summary>
        /// Preenchido pelo controller a partir dos dados da conta (nunca a
        /// partir do formulário): indica se é uma alteração ou a definição
        /// da primeira password.
        /// </summary>
        public bool TemPasswordLocal { get; set; } = true;

        [Display(Name = "Password atual")]
        [DataType(DataType.Password)]
        public string? PasswordAtual { get; set; }

        [Display(Name = "Nova password")]
        [Required(ErrorMessage = "A nova password é obrigatória.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A password deve ter pelo menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "A password deve incluir pelo menos 1 letra maiúscula, 1 número e 1 símbolo (ex.: ! ou ?).")]
        public string NovaPassword { get; set; } = string.Empty;

        [Display(Name = "Confirmar nova password")]
        [Required(ErrorMessage = "Confirme a nova password.")]
        [Compare(nameof(NovaPassword), ErrorMessage = "As passwords não coincidem.")]
        [DataType(DataType.Password)]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
