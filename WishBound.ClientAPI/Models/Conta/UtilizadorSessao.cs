namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>
    /// Dados públicos de um utilizador devolvidos pela WebAPI
    /// (espelho do DTO UtilizadorResposta da API).
    /// Usado para criar a sessão (cookie) e mostrar o perfil.
    /// </summary>
    public class UtilizadorSessao
    {
        public int Id { get; set; }
        public string NomeUtilizador { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailValidado { get; set; }
        public bool IsAdmin { get; set; }

        /// <summary>
        /// false nas contas criadas com Google (entram sempre pelo Google e
        /// ainda não definiram uma password local).
        /// </summary>
        public bool TemPasswordLocal { get; set; }
        public string? FotoPerfilUrl { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? UltimoLogin { get; set; }
    }
}
