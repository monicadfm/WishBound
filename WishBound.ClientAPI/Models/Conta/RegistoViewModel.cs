using System.ComponentModel.DataAnnotations;

namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>Formulário de registo de uma nova conta.</summary>
    public class RegistoViewModel
    {
        [Display(Name = "Nome de utilizador")]
        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome de utilizador deve ter entre 3 e 50 caracteres.")]
        public string NomeUtilizador { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email não é válido.")]
        [StringLength(150, ErrorMessage = "O email não pode ter mais de 150 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Password")]
        [Required(ErrorMessage = "A password é obrigatória.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A password deve ter pelo menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[0-9])(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "A password deve incluir pelo menos 1 letra maiúscula, 1 número e 1 símbolo (ex.: ! ou ?).")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Confirmar password")]
        [Required(ErrorMessage = "Confirme a password.")]
        [Compare(nameof(Password), ErrorMessage = "As passwords não coincidem.")]
        [DataType(DataType.Password)]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
