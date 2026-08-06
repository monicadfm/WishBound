namespace WishBound.ClientAPI.Models.Conta
{
    /// <summary>
    /// Resposta da WebAPI às operações que geram um token
    /// (registo, reenvio de validação, recuperação de password).
    /// Em modo de desenvolvimento o token vem na resposta para o
    /// site construir e mostrar o link no ecrã (sem envio de email).
    /// </summary>
    public class TokenResposta
    {
        public string Mensagem { get; set; } = string.Empty;
        public string? Token { get; set; }
    }
}
