using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;
using WishBound.WebAPI.Services;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Sistema de contas e autenticação:
    ///   - Registo de utilizadores (INSERT)
    ///   - Login (verificação de credenciais)
    ///   - Validação de conta através de email (token)
    ///   - Recuperação e reposição de password (token)
    ///   - Alteração de password
    ///   - Gestão de perfil (UPDATE)
    ///
    /// As passwords são guardadas apenas como hash PBKDF2
    /// (ver Services/PasswordHasher.cs).
    ///
    /// EMAILS: com um servidor SMTP configurado (secção "Email" da
    /// configuração — ver Services/ServicoEmail.cs), os links de validação de
    /// conta e de recuperação de password são enviados por email e o token
    /// NÃO volta na resposta. Sem SMTP configurado (ou se o envio falhar),
    /// mantém-se o modo de desenvolvimento: o token vem na resposta e o site
    /// mostra o link no ecrã.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ContaController : ControllerBase
    {
        // Prefixos que distinguem os dois tipos de token guardados
        // na tabela TokensRecuperacaoPassword (ver o modelo).
        private const string PrefixoValidacaoEmail = "EV.";
        private const string PrefixoRecuperacaoPassword = "RP.";

        private static readonly TimeSpan ValidadeTokenEmail = TimeSpan.FromHours(24);
        private static readonly TimeSpan ValidadeTokenRecuperacao = TimeSpan.FromHours(1);

        private readonly WishBoundContext _contexto;
        private readonly IServicoEmail _email;
        private readonly IConfiguration _configuracao;

        public ContaController(WishBoundContext contexto, IServicoEmail email, IConfiguration configuracao)
        {
            _contexto = contexto;
            _email = email;
            _configuracao = configuracao;
        }

        // ------------------------------------------------------------
        // POST: api/conta/registar  (INSERT - novo utilizador)
        // ------------------------------------------------------------
        [HttpPost("registar")]
        public async Task<ActionResult<TokenResposta>> Registar(RegistoPedido pedido)
        {
            try
            {
                // Unicidade do nome de utilizador e do email (validação de dados de entrada)
                if (await _contexto.Utilizadores.AnyAsync(u => u.NomeUtilizador == pedido.NomeUtilizador))
                {
                    return Conflict("Já existe uma conta com esse nome de utilizador.");
                }

                if (await _contexto.Utilizadores.AnyAsync(u => u.Email == pedido.Email))
                {
                    return Conflict("Já existe uma conta com esse email.");
                }

                var utilizador = new Utilizador
                {
                    NomeUtilizador = pedido.NomeUtilizador,
                    Email = pedido.Email,
                    PasswordHash = PasswordHasher.GerarHash(pedido.Password),
                    EmailValidado = false,
                    IsAdmin = false,
                    IsAtivo = true,
                    DataCriacao = DateTime.UtcNow
                };

                // TRANSAÇÃO: ou se cria tudo (utilizador + inventário +
                // carteiras + token) ou não se cria nada. Sem isto, uma
                // falha a meio deixaria uma conta "meia-criada" impossível
                // de registar de novo.
                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                _contexto.Utilizadores.Add(utilizador);
                await _contexto.SaveChangesAsync();

                // Cria também o inventário e as carteiras (uma por tipo de moeda),
                // para que o utilizador fique pronto para as funcionalidades de
                // coleção e economia. Feito com SQL direto porque estas tabelas
                // ainda não têm entidades EF mapeadas.
                await _contexto.Database.ExecuteSqlAsync(
                    $"INSERT INTO InventarioUtilizador (UtilizadorId, CapacidadeBase, CapacidadeExtra) VALUES ({utilizador.Id}, 100, 0)");
                await _contexto.Database.ExecuteSqlAsync(
                    $"INSERT INTO CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo) SELECT {utilizador.Id}, TipoMoedaId, 0 FROM TiposMoeda");

                // Token de validação de email
                string token = await CriarTokenAsync(utilizador.Id, PrefixoValidacaoEmail, ValidadeTokenEmail);

                await transacao.CommitAsync();

                // MODO DE DESENVOLVIMENTO = não há servidor de email configurado.
                // Só nesse caso o token volta na resposta para o site mostrar o
                // link no ecrã. Se HÁ email configurado mas o envio falha, o
                // token NÃO é devolvido (senão bastava desligar o servidor de
                // email para ver os links de outras contas).
                bool modoDev = !_email.Configurado;
                bool enviado = await EnviarEmailValidacaoAsync(utilizador, token);

                return Ok(new TokenResposta
                {
                    Mensagem = modoDev
                        ? "Conta criada com sucesso. Valide o seu email para poder iniciar sessão."
                        : enviado
                            ? "Conta criada com sucesso. Enviámos um email para " + utilizador.Email +
                              " com o link de validação (verifique também o spam)."
                            : "Conta criada com sucesso, mas não foi possível enviar o email de validação. " +
                              "Tente daqui a pouco a opção \"Reenviar validação\".",
                    Token = modoDev ? token : null,
                    EmailEnviado = enviado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao registar o utilizador: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/conta/login  (verificação de credenciais)
        // ------------------------------------------------------------
        [HttpPost("login")]
        public async Task<ActionResult<UtilizadorResposta>> Login(LoginPedido pedido)
        {
            try
            {
                // Aceita nome de utilizador OU email como identificador
                var utilizador = await _contexto.Utilizadores.FirstOrDefaultAsync(
                    u => u.NomeUtilizador == pedido.Identificador || u.Email == pedido.Identificador);

                // Mensagem genérica: não revelamos se o que falhou foi o
                // identificador ou a password (boa prática de segurança).
                if (utilizador == null || !PasswordHasher.Verificar(pedido.Password, utilizador.PasswordHash))
                {
                    return Unauthorized("Credenciais inválidas.");
                }

                if (!utilizador.IsAtivo)
                {
                    return Unauthorized("Esta conta está desativada.");
                }

                if (!utilizador.EmailValidado)
                {
                    return Unauthorized("O email desta conta ainda não foi validado. Use a opção \"Reenviar validação\".");
                }

                utilizador.UltimoLogin = DateTime.UtcNow;
                await _contexto.SaveChangesAsync();

                return Ok(ParaResposta(utilizador));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao iniciar sessão: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/conta/login-google  (login/registo com conta Google)
        // ------------------------------------------------------------
        // O ClientAPI chama este endpoint depois de a Google confirmar a
        // identidade do utilizador (OAuth 2.0). Três cenários possíveis:
        //   1. Já existe conta com este GoogleId        -> inicia sessão;
        //   2. Existe conta com o mesmo email           -> liga o GoogleId
        //      a essa conta (e valida o email, porque a Google confirmou-o);
        //   3. Não existe nenhuma conta                 -> cria uma conta
        //      nova SEM password local (PasswordHash = NULL).
        [HttpPost("login-google")]
        public async Task<ActionResult<UtilizadorResposta>> LoginGoogle(LoginGooglePedido pedido)
        {
            try
            {
                // Cenário 1: conta já ligada a esta conta Google
                var utilizador = await _contexto.Utilizadores.FirstOrDefaultAsync(
                    u => u.GoogleId == pedido.GoogleId);

                // Cenário 2: conta local com o mesmo email -> ligar as duas
                if (utilizador == null)
                {
                    utilizador = await _contexto.Utilizadores.FirstOrDefaultAsync(
                        u => u.Email == pedido.Email);

                    if (utilizador != null)
                    {
                        utilizador.GoogleId = pedido.GoogleId;
                        // A Google já confirmou que o email pertence ao utilizador,
                        // por isso a conta fica validada (mesmo que nunca tenha
                        // aberto o nosso link de validação).
                        utilizador.EmailValidado = true;
                    }
                }

                if (utilizador != null)
                {
                    if (!utilizador.IsAtivo)
                    {
                        return Unauthorized("Esta conta está desativada.");
                    }

                    // Aproveita a foto da conta Google se ainda não tiver nenhuma
                    if (string.IsNullOrEmpty(utilizador.FotoPerfilUrl) &&
                        !string.IsNullOrEmpty(pedido.FotoUrl))
                    {
                        utilizador.FotoPerfilUrl = pedido.FotoUrl;
                    }

                    utilizador.UltimoLogin = DateTime.UtcNow;
                    await _contexto.SaveChangesAsync();

                    return Ok(ParaResposta(utilizador));
                }

                // Cenário 3: primeira vez -> criar conta nova (registo Google).
                // Mesma transação do registo normal: utilizador + inventário +
                // carteiras, tudo ou nada.
                var novo = new Utilizador
                {
                    NomeUtilizador = await GerarNomeUtilizadorUnicoAsync(pedido.Nome, pedido.Email),
                    Email = pedido.Email,
                    PasswordHash = null,        // sem password local: entra sempre com Google
                    GoogleId = pedido.GoogleId,
                    EmailValidado = true,       // email confirmado pela Google
                    FotoPerfilUrl = pedido.FotoUrl,
                    IsAdmin = false,
                    IsAtivo = true,
                    DataCriacao = DateTime.UtcNow,
                    UltimoLogin = DateTime.UtcNow
                };

                await using var transacao = await _contexto.Database.BeginTransactionAsync();

                _contexto.Utilizadores.Add(novo);
                await _contexto.SaveChangesAsync();

                await _contexto.Database.ExecuteSqlAsync(
                    $"INSERT INTO InventarioUtilizador (UtilizadorId, CapacidadeBase, CapacidadeExtra) VALUES ({novo.Id}, 100, 0)");
                await _contexto.Database.ExecuteSqlAsync(
                    $"INSERT INTO CarteirasUtilizador (UtilizadorId, TipoMoedaId, Saldo) SELECT {novo.Id}, TipoMoedaId, 0 FROM TiposMoeda");

                await transacao.CommitAsync();

                return Ok(ParaResposta(novo));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao iniciar sessão com Google: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/conta/validar-email  (consome o token "EV.")
        // ------------------------------------------------------------
        [HttpPost("validar-email")]
        public async Task<IActionResult> ValidarEmail(ValidarEmailPedido pedido)
        {
            try
            {
                // Caso especial: o token já foi usado mas o email ficou validado
                // (ex.: o utilizador atualizou a página do link). Em vez de um
                // erro confuso, confirmamos que está tudo bem.
                var tokenUsado = await _contexto.TokensRecuperacao
                    .Include(t => t.Utilizador)
                    .FirstOrDefaultAsync(t => t.Token == pedido.Token && t.Utilizado);

                if (tokenUsado?.Utilizador?.EmailValidado == true)
                {
                    return Ok("O email já tinha sido validado. Pode iniciar sessão.");
                }

                var token = await ObterTokenValidoAsync(pedido.Token, PrefixoValidacaoEmail);
                if (token == null)
                {
                    return BadRequest("O link de validação é inválido, já foi usado ou expirou.");
                }

                var utilizador = await _contexto.Utilizadores.FindAsync(token.UtilizadorId);
                if (utilizador == null)
                {
                    return BadRequest("O utilizador associado ao link já não existe.");
                }

                utilizador.EmailValidado = true;
                token.Utilizado = true;
                await _contexto.SaveChangesAsync();

                return Ok("Email validado com sucesso. Já pode iniciar sessão.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao validar o email: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/conta/reenviar-validacao  (novo token "EV.")
        // ------------------------------------------------------------
        [HttpPost("reenviar-validacao")]
        public async Task<ActionResult<TokenResposta>> ReenviarValidacao(EmailPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.FirstOrDefaultAsync(
                    u => u.Email == pedido.Email && u.IsAtivo);

                // Resposta neutra: não revelamos se o email existe na base de dados
                if (utilizador == null || utilizador.EmailValidado)
                {
                    return Ok(new TokenResposta
                    {
                        Mensagem = "Se o email corresponder a uma conta por validar, foi gerado um novo link."
                    });
                }

                string token = await CriarTokenAsync(utilizador.Id, PrefixoValidacaoEmail, ValidadeTokenEmail);

                bool modoDev = !_email.Configurado;
                bool enviado = await EnviarEmailValidacaoAsync(utilizador, token);

                return Ok(new TokenResposta
                {
                    // Com email configurado a resposta é sempre a mesma, exista
                    // ou não a conta (ver o caso acima): não revelamos nada.
                    Mensagem = modoDev
                        ? "Novo link de validação gerado."
                        : "Se o email corresponder a uma conta por validar, foi enviado um novo link.",
                    Token = modoDev ? token : null,
                    EmailEnviado = enviado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao reenviar a validação: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/conta/recuperar-password  (novo token "RP.")
        // ------------------------------------------------------------
        [HttpPost("recuperar-password")]
        public async Task<ActionResult<TokenResposta>> RecuperarPassword(EmailPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.FirstOrDefaultAsync(
                    u => u.Email == pedido.Email && u.IsAtivo);

                // Resposta neutra: não revelamos se o email existe na base de dados
                if (utilizador == null)
                {
                    return Ok(new TokenResposta
                    {
                        Mensagem = "Se o email corresponder a uma conta, foi gerado um link de recuperação."
                    });
                }

                string token = await CriarTokenAsync(utilizador.Id, PrefixoRecuperacaoPassword, ValidadeTokenRecuperacao);

                bool modoDev = !_email.Configurado;
                bool enviado = await EnviarEmailRecuperacaoAsync(utilizador, token);

                return Ok(new TokenResposta
                {
                    // Mesma resposta exista ou não a conta (ver o caso acima)
                    Mensagem = modoDev
                        ? "Link de recuperação gerado."
                        : "Se o email corresponder a uma conta, foi enviado um link de recuperação.",
                    Token = modoDev ? token : null,
                    EmailEnviado = enviado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao pedir a recuperação de password: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/conta/repor-password  (consome o token "RP.")
        // ------------------------------------------------------------
        [HttpPost("repor-password")]
        public async Task<IActionResult> ReporPassword(ReporPasswordPedido pedido)
        {
            try
            {
                var token = await ObterTokenValidoAsync(pedido.Token, PrefixoRecuperacaoPassword);
                if (token == null)
                {
                    return BadRequest("O link de recuperação é inválido, já foi usado ou expirou.");
                }

                var utilizador = await _contexto.Utilizadores.FindAsync(token.UtilizadorId);
                if (utilizador == null)
                {
                    return BadRequest("O utilizador associado ao link já não existe.");
                }

                utilizador.PasswordHash = PasswordHasher.GerarHash(pedido.NovaPassword);
                // Repor a password através do email também prova que o email é do utilizador
                utilizador.EmailValidado = true;
                token.Utilizado = true;
                await _contexto.SaveChangesAsync();

                return Ok("Password alterada com sucesso. Já pode iniciar sessão com a nova password.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao repor a password: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // POST: api/conta/alterar-password  (utilizador autenticado)
        // ------------------------------------------------------------
        // Serve para dois casos:
        //   - ALTERAR: conta normal, tem de confirmar a password atual;
        //   - DEFINIR: conta criada com Google (PasswordHash NULL), que nunca
        //     teve password local — aqui não há password atual para confirmar.
        //     A identidade já foi provada pela Google no início da sessão.
        [HttpPost("alterar-password")]
        public async Task<IActionResult> AlterarPassword(AlterarPasswordPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.FindAsync(pedido.UtilizadorId);
                if (utilizador == null || !utilizador.IsAtivo)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                bool temPasswordLocal = !string.IsNullOrEmpty(utilizador.PasswordHash);

                if (temPasswordLocal)
                {
                    if (string.IsNullOrEmpty(pedido.PasswordAtual))
                    {
                        return BadRequest("A password atual é obrigatória.");
                    }

                    if (!PasswordHasher.Verificar(pedido.PasswordAtual, utilizador.PasswordHash))
                    {
                        return Unauthorized("A password atual está incorreta.");
                    }
                }

                utilizador.PasswordHash = PasswordHasher.GerarHash(pedido.NovaPassword);
                await _contexto.SaveChangesAsync();

                return Ok(temPasswordLocal
                    ? "Password alterada com sucesso."
                    : "Password definida com sucesso. A partir de agora também pode entrar com o nome de utilizador e password.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao alterar a password: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // GET: api/conta/5  (dados públicos de um utilizador)
        // ------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<UtilizadorResposta>> ObterUtilizador(int id)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.FindAsync(id);
                if (utilizador == null)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                return Ok(ParaResposta(utilizador));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao obter o utilizador: " + ex.Message);
            }
        }

        // ------------------------------------------------------------
        // PUT: api/conta/perfil  (UPDATE - gestão de perfil)
        // ------------------------------------------------------------
        [HttpPut("perfil")]
        public async Task<ActionResult<UtilizadorResposta>> AtualizarPerfil(PerfilPedido pedido)
        {
            try
            {
                var utilizador = await _contexto.Utilizadores.FindAsync(pedido.UtilizadorId);
                if (utilizador == null || !utilizador.IsAtivo)
                {
                    return NotFound("Utilizador não encontrado.");
                }

                // O novo nome de utilizador não pode pertencer a outra conta
                if (await _contexto.Utilizadores.AnyAsync(
                        u => u.NomeUtilizador == pedido.NomeUtilizador && u.Id != pedido.UtilizadorId))
                {
                    return Conflict("Já existe uma conta com esse nome de utilizador.");
                }

                utilizador.NomeUtilizador = pedido.NomeUtilizador;
                utilizador.FotoPerfilUrl = string.IsNullOrWhiteSpace(pedido.FotoPerfilUrl)
                    ? null
                    : pedido.FotoPerfilUrl;

                await _contexto.SaveChangesAsync();

                return Ok(ParaResposta(utilizador));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao atualizar o perfil: " + ex.Message);
            }
        }

        // ============================================================
        //  Métodos auxiliares
        // ============================================================

        /// <summary>Gera um token aleatório seguro, guarda-o e devolve o seu valor.</summary>
        private async Task<string> CriarTokenAsync(int utilizadorId, string prefixo, TimeSpan validade)
        {
            // 32 bytes aleatórios em Base64Url (seguro para usar em links)
            string valor = prefixo + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            _contexto.TokensRecuperacao.Add(new TokenRecuperacaoPassword
            {
                UtilizadorId = utilizadorId,
                Token = valor,
                DataCriacao = DateTime.UtcNow,
                DataExpiracao = DateTime.UtcNow.Add(validade),
                Utilizado = false
            });

            await _contexto.SaveChangesAsync();
            return valor;
        }

        /// <summary>Procura um token não usado, não expirado e do tipo certo (null se inválido).</summary>
        private async Task<TokenRecuperacaoPassword?> ObterTokenValidoAsync(string valor, string prefixo)
        {
            if (string.IsNullOrWhiteSpace(valor) || !valor.StartsWith(prefixo))
            {
                return null;
            }

            return await _contexto.TokensRecuperacao.FirstOrDefaultAsync(
                t => t.Token == valor && !t.Utilizado && t.DataExpiracao > DateTime.UtcNow);
        }

        /// <summary>
        /// Gera um nome de utilizador único a partir do nome da conta Google
        /// (ou do email, se não houver nome). Se já existir, acrescenta um
        /// número: "Luna", "Luna2", "Luna3", ...
        /// </summary>
        private async Task<string> GerarNomeUtilizadorUnicoAsync(string? nome, string email)
        {
            // Base: nome Google sem espaços, ou a parte do email antes do "@"
            string baseNome = string.IsNullOrWhiteSpace(nome)
                ? email.Split('@')[0]
                : nome.Replace(" ", "");

            // Respeita as regras do NomeUtilizador (3 a 50 caracteres)
            if (baseNome.Length > 47)
            {
                baseNome = baseNome[..47]; // deixa espaço para o sufixo numérico
            }

            while (baseNome.Length < 3)
            {
                baseNome += "0";
            }

            string candidato = baseNome;
            int sufixo = 2;

            while (await _contexto.Utilizadores.AnyAsync(u => u.NomeUtilizador == candidato))
            {
                candidato = baseNome + sufixo;
                sufixo++;
            }

            return candidato;
        }

        /// <summary>
        /// Constrói um link do SITE a partir do endereço configurado em
        /// "Site:BaseUrl" (ex.: http://localhost:5100/Conta/ValidarEmail?token=...).
        /// </summary>
        private string ConstruirLink(string caminho, string token)
        {
            string baseUrl = (_configuracao["Site:BaseUrl"] ?? "http://localhost:5100").TrimEnd('/');
            return baseUrl + caminho + "?token=" + Uri.EscapeDataString(token);
        }

        /// <summary>Email com o link de validação de conta. false = não foi enviado.</summary>
        private Task<bool> EnviarEmailValidacaoAsync(Utilizador utilizador, string token)
        {
            string link = ConstruirLink("/Conta/ValidarEmail", token);

            return _email.EnviarAsync(
                utilizador.Email,
                "WishBound - confirme o seu email",
                CorpoEmail(
                    "Bem-vindo(a) ao WishBound, " + WebUtility.HtmlEncode(utilizador.NomeUtilizador) + "!",
                    "Falta só confirmar o seu email para poder iniciar sessão.",
                    "Confirmar email",
                    link,
                    "Este link é válido durante 24 horas. Se não foi você que criou a conta, ignore este email."));
        }

        /// <summary>Email com o link de recuperação de password. false = não foi enviado.</summary>
        private Task<bool> EnviarEmailRecuperacaoAsync(Utilizador utilizador, string token)
        {
            string link = ConstruirLink("/Conta/ReporPassword", token);

            return _email.EnviarAsync(
                utilizador.Email,
                "WishBound - recuperação de password",
                CorpoEmail(
                    "Olá, " + WebUtility.HtmlEncode(utilizador.NomeUtilizador) + "!",
                    "Recebemos um pedido para definir uma nova password na sua conta WishBound.",
                    "Definir nova password",
                    link,
                    "Este link é válido durante 1 hora. Se não foi você que fez o pedido, ignore este email — a password atual continua válida."));
        }

        /// <summary>Corpo HTML simples e igual para todos os emails da plataforma.</summary>
        private static string CorpoEmail(string titulo, string texto, string textoBotao, string link, string rodape)
        {
            return $@"<div style=""font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:0 auto;color:#1f2430"">
  <h2 style=""color:#6d5bd0"">{titulo}</h2>
  <p>{texto}</p>
  <p style=""margin:28px 0"">
    <a href=""{link}"" style=""background:#6d5bd0;color:#fff;padding:12px 22px;border-radius:8px;text-decoration:none"">{textoBotao}</a>
  </p>
  <p style=""font-size:13px;color:#5b6472"">Se o botão não funcionar, copie este endereço para o browser:<br />{link}</p>
  <hr style=""border:none;border-top:1px solid #e3e6ee"" />
  <p style=""font-size:12px;color:#8a91a0"">{rodape}</p>
  <p style=""font-size:12px;color:#8a91a0"">WishBound &middot; Plataforma de Coleção Digital de Personagens</p>
</div>";
        }

        /// <summary>Converte a entidade Utilizador na resposta pública (sem PasswordHash).</summary>
        private static UtilizadorResposta ParaResposta(Utilizador u)
        {
            return new UtilizadorResposta
            {
                Id = u.Id,
                NomeUtilizador = u.NomeUtilizador,
                Email = u.Email,
                EmailValidado = u.EmailValidado,
                IsAdmin = u.IsAdmin,
                TemPasswordLocal = !string.IsNullOrEmpty(u.PasswordHash),
                FotoPerfilUrl = u.FotoPerfilUrl,
                DataCriacao = u.DataCriacao,
                UltimoLogin = u.UltimoLogin
            };
        }
    }
}
