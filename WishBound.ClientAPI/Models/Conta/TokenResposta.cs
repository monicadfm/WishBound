namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>
    /// Resposta da WebAPI às operações que geram um token
    /// (registo, reenvio de validação, recuperação de password).
    /// Com SMTP configurado na API, o link vai por email e o token vem
    /// null. Sem SMTP (modo de desenvolvimento), o token vem na resposta
    /// para o site construir e mostrar o link no ecrã.
    /// </summary>
    public class TokenResposta
    {
        public string Mensagem { get; set; } = string.Empty;

        /// <summary>Só vem preenchido quando não houve envio de email (modo dev).</summary>
        public string? Token { get; set; }

        /// <summary>true quando a API enviou mesmo o email.</summary>
        public bool EmailEnviado { get; set; }
    }
}
