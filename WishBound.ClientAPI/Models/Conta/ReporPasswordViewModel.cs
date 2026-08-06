using System.ComponentModel.DataAnnotations;

namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>Formulário de reposição de password (com o token do link recebido).</summary>
    public class ReporPasswordViewModel
    {
        // Token vem no link (campo escondido no formulário)
        [Required]
        public string Token { get; set; } = string.Empty;

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
