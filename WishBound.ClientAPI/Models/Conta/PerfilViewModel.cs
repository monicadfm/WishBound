using System.ComponentModel.DataAnnotations;

namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>Formulário de gestão de perfil.</summary>
    public class PerfilViewModel
    {
        [Display(Name = "Nome de utilizador")]
        [Required(ErrorMessage = "O nome de utilizador é obrigatório.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "O nome de utilizador deve ter entre 3 e 50 caracteres.")]
        public string NomeUtilizador { get; set; } = string.Empty;

        [Display(Name = "Foto de perfil (caminho ou URL)")]
        [StringLength(255, ErrorMessage = "O caminho da foto não pode ter mais de 255 caracteres.")]
        public string? FotoPerfilUrl { get; set; }

        // Campos apenas de leitura, mostrados na página
        public string Email { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? UltimoLogin { get; set; }
        public bool IsAdmin { get; set; }
    }
}
