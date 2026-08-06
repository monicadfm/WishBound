using System.ComponentModel.DataAnnotations;

namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>
    /// Formulário que pede apenas o email da conta.
    /// Usado na recuperação de password e no reenvio da validação de email.
    /// </summary>
    public class RecuperarPasswordViewModel
    {
        [Display(Name = "Email da conta")]
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email não é válido.")]
        public string Email { get; set; } = string.Empty;
    }
}
