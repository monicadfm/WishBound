using System.ComponentModel.DataAnnotations;

namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>Formulário de alteração de password (utilizador autenticado).</summary>
    public class AlterarPasswordViewModel
    {
        [Display(Name = "Password atual")]
        [Required(ErrorMessage = "A password atual é obrigatória.")]
        [DataType(DataType.Password)]
        public string PasswordAtual { get; set; } = string.Empty;

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
