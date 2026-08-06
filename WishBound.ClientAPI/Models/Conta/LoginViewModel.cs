using System.ComponentModel.DataAnnotations;

namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>Formulário de início de sessão.</summary>
    public class LoginViewModel
    {
        [Display(Name = "Nome de utilizador ou email")]
        [Required(ErrorMessage = "Indique o nome de utilizador ou o email.")]
        public string Identificador { get; set; } = string.Empty;

        [Display(Name = "Password")]
        [Required(ErrorMessage = "A password é obrigatória.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Manter sessão iniciada")]
        public bool Lembrar { get; set; }
    }
}
