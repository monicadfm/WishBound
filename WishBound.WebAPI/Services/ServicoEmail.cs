using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WishBound.WebAPI.Services
{
    /// <summary>
    /// Definições do servidor de email (secção "Email" da configuração).
    /// Os valores reais NUNCA ficam no appsettings.json versionado: são
    /// escritos em appsettings.Local.json (ficheiro no .gitignore).
    /// </summary>
    public class DefinicoesEmail
    {
        /// <summary>Servidor SMTP (ex.: smtp.gmail.com). Vazio = envio de emails desligado.</summary>
        public string? Host { get; set; }

        public int Porta { get; set; } = 587;

        /// <summary>Conta usada para autenticar no servidor SMTP.</summary>
        public string? Utilizador { get; set; }

        /// <summary>Password (ou "password de aplicação", no caso do Gmail).</summary>
        public string? Password { get; set; }

        /// <summary>Endereço que aparece como remetente.</summary>
        public string? Remetente { get; set; }

        public string NomeRemetente { get; set; } = "WishBound";

        /// <summary>STARTTLS (porta 587). Praticamente todos os servidores exigem.</summary>
        public bool UsarSsl { get; set; } = true;

        /// <summary>Só há envio real de emails se houver servidor e remetente.</summary>
        public bool Configurado =>
            !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(Remetente);
    }

    /// <summary>Envio de emails da plataforma (validação de conta, recuperação de password).</summary>
    public interface IServicoEmail
    {
        /// <summary>true quando existe configuração SMTP (senão o site mostra o link no ecrã).</summary>
        bool Configurado { get; }

        /// <summary>Envia um email. Devolve false se não houver configuração ou se o envio falhar.</summary>
        Task<bool> EnviarAsync(string destinatario, string assunto, string corpoHtml);
    }

    /// <summary>
    /// Implementação com o SmtpClient do .NET (não precisa de pacotes extra).
    ///
    /// MODO DE DESENVOLVIMENTO: sem definições de SMTP, o serviço não envia
    /// nada e devolve false — nesse caso a API continua a devolver o token
    /// para o site mostrar o link no ecrã, como antes.
    /// </summary>
    public class ServicoEmailSmtp : IServicoEmail
    {
        private readonly DefinicoesEmail _definicoes;
        private readonly ILogger<ServicoEmailSmtp> _registo;

        public ServicoEmailSmtp(IOptions<DefinicoesEmail> definicoes, ILogger<ServicoEmailSmtp> registo)
        {
            _definicoes = definicoes.Value;
            _registo = registo;
        }

        public bool Configurado => _definicoes.Configurado;

        public async Task<bool> EnviarAsync(string destinatario, string assunto, string corpoHtml)
        {
            if (!Configurado)
            {
                return false;
            }

            try
            {
                using var cliente = new SmtpClient(_definicoes.Host, _definicoes.Porta)
                {
                    EnableSsl = _definicoes.UsarSsl,
                    Timeout = 8000
                };

                // Servidores que não pedem autenticação (ex.: Papercut, MailHog
                // usados em desenvolvimento) ficam sem credenciais.
                if (!string.IsNullOrWhiteSpace(_definicoes.Utilizador))
                {
                    cliente.Credentials = new NetworkCredential(_definicoes.Utilizador, _definicoes.Password);
                }

                using var mensagem = new MailMessage
                {
                    From = new MailAddress(_definicoes.Remetente!, _definicoes.NomeRemetente),
                    Subject = assunto,
                    Body = corpoHtml,
                    IsBodyHtml = true
                };
                mensagem.To.Add(destinatario);

                // Limite de tempo garantido: o site espera no máximo 15 segundos
                // pela API, por isso um servidor de email lento não pode fazer
                // o registo parecer que falhou (a conta já foi criada).
                await cliente.SendMailAsync(mensagem).WaitAsync(TimeSpan.FromSeconds(10));

                _registo.LogInformation("Email enviado para {Destinatario}: {Assunto}", destinatario, assunto);
                return true;
            }
            catch (Exception ex)
            {
                // Um email que falha não pode partir o registo nem a recuperação
                // de password: o erro fica registado e a API devolve o token
                // para o site mostrar o link no ecrã.
                _registo.LogError(ex, "Falha ao enviar o email para {Destinatario}.", destinatario);
                return false;
            }
        }
    }
}
