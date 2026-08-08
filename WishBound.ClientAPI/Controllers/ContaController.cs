using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WishBound.ClientAPI.Models.Conta;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Conta de utilizador: registo, login/logout, validação de email,
    /// recuperação/alteração de password e gestão de perfil.
    ///
    /// A verificação de credenciais é feita pela WebAPI; este controller
    /// apenas cria/destrói a SESSÃO local (cookie de autenticação) e
    /// apresenta os formulários.
    ///
    /// NOTA (modo de desenvolvimento): como ainda não há envio real de
    /// emails, os links de validação/recuperação são mostrados no ecrã
    /// numa caixa destacada (TempData["LinkDev"]).
    /// </summary>
    public class ContaController : Controller
    {
        private readonly WishBoundApiService _api;
        private readonly IConfiguration _configuracao;

        public ContaController(WishBoundApiService api, IConfiguration configuracao)
        {
            _api = api;
            _configuracao = configuracao;
        }

        /// <summary>Há credenciais Google no appsettings.json?</summary>
        private bool GoogleConfigurado()
        {
            return !string.IsNullOrWhiteSpace(_configuracao["Autenticacao:Google:ClientId"]) &&
                   !string.IsNullOrWhiteSpace(_configuracao["Autenticacao:Google:ClientSecret"]);
        }

        // ------------------------------------------------------------
        // Login / Logout
        // ------------------------------------------------------------

        // GET: /Conta/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Se já tem sessão iniciada, não faz sentido mostrar o login
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Conta/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            try
            {
                var (utilizador, erro) = await _api.LoginAsync(modelo.Identificador, modelo.Password);

                if (utilizador == null)
                {
                    ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(erro)
                        ? "Credenciais inválidas."
                        : erro);
                    return View(modelo);
                }

                // Cria o cookie de autenticação com os dados ("claims") do utilizador
                await IniciarSessaoAsync(utilizador, modelo.Lembrar);

                TempData["Sucesso"] = "Bem-vindo(a), " + utilizador.NomeUtilizador + "!";

                // Volta à página que exigiu login (se existir e for local)
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,
                    "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.");
                return View(modelo);
            }
        }

        // POST: /Conta/Logout  (encerramento de sessão)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Sucesso"] = "Sessão terminada. Até à próxima!";
            return RedirectToAction("Index", "Home");
        }

        // ------------------------------------------------------------
        // Login com Google (OAuth 2.0)
        // ------------------------------------------------------------
        // Fluxo completo:
        //   1. O utilizador carrega em "Entrar com Google" (POST LoginGoogle);
        //   2. O site redireciona para a página de login da Google (Challenge);
        //   3. A Google confirma a identidade e devolve o utilizador ao site
        //      (o resultado fica no cookie temporário "Externo");
        //   4. O GoogleCallback lê esses dados, pede à WebAPI para iniciar
        //      sessão (criando/ligando a conta se necessário) e cria o
        //      cookie de sessão normal.

        // POST: /Conta/LoginGoogle  (passo 1 - arranca o fluxo OAuth)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LoginGoogle(string? returnUrl = null)
        {
            // Se as credenciais Google não estiverem configuradas no
            // appsettings.json, o botão nem aparece - isto é só uma rede
            // de segurança para pedidos feitos "à mão".
            if (!GoogleConfigurado())
            {
                TempData["Erro"] = "O login com Google não está configurado neste servidor.";
                return RedirectToAction(nameof(Login));
            }

            // Depois de a Google autenticar, volta ao GoogleCallback
            var propriedades = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl })
            };

            return Challenge(propriedades, "Google");
        }

        // GET: /Conta/GoogleCallback  (passo 4 - regresso da Google)
        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            // Lê o resultado do OAuth guardado no cookie temporário
            var resultado = await HttpContext.AuthenticateAsync("Externo");

            if (!resultado.Succeeded || resultado.Principal == null)
            {
                TempData["Erro"] = "Não foi possível iniciar sessão com o Google.";
                return RedirectToAction(nameof(Login));
            }

            // Dados confirmados pela Google
            string? googleId = resultado.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            string? email = resultado.Principal.FindFirstValue(ClaimTypes.Email);
            string? nome = resultado.Principal.FindFirstValue(ClaimTypes.Name);
            string? foto = resultado.Principal.FindFirstValue("urn:google:foto");

            // Limites das colunas na base de dados: se o link da foto for
            // maior do que 255 caracteres é mais seguro ignorá-lo do que
            // falhar o login; o nome é apenas encurtado (máx. 100).
            if (foto?.Length > 255)
            {
                foto = null;
            }

            if (nome?.Length > 100)
            {
                nome = nome[..100];
            }

            // O cookie temporário já cumpriu a sua função
            await HttpContext.SignOutAsync("Externo");

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                TempData["Erro"] = "A conta Google não devolveu os dados necessários (id e email).";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                // A WebAPI inicia sessão, ligando ou criando a conta se necessário
                var (utilizador, erro) = await _api.LoginGoogleAsync(googleId, email, nome, foto);

                if (utilizador == null)
                {
                    TempData["Erro"] = string.IsNullOrWhiteSpace(erro)
                        ? "Não foi possível iniciar sessão com o Google."
                        : erro;
                    return RedirectToAction(nameof(Login));
                }

                await IniciarSessaoAsync(utilizador, lembrar: false);

                TempData["Sucesso"] = "Bem-vindo(a), " + utilizador.NomeUtilizador + "!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Login));
            }
        }

        // ------------------------------------------------------------
        // Registo
        // ------------------------------------------------------------

        // GET: /Conta/Registar
        [HttpGet]
        public IActionResult Registar()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegistoViewModel());
        }

        // POST: /Conta/Registar  (INSERT via API)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registar(RegistoViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            try
            {
                var (resposta, erro) = await _api.RegistarAsync(modelo);

                if (resposta == null)
                {
                    ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(erro)
                        ? "Não foi possível criar a conta."
                        : erro);
                    return View(modelo);
                }

                TempData["Sucesso"] = resposta.Mensagem;
                GuardarLinkDev(resposta.Token, "ValidarEmail", "Link de validação de email");

                return RedirectToAction(nameof(Login));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,
                    "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.");
                return View(modelo);
            }
        }

        // ------------------------------------------------------------
        // Validação de email
        // ------------------------------------------------------------

        // GET: /Conta/ValidarEmail?token=EV....  (o "link do email")
        [HttpGet]
        public async Task<IActionResult> ValidarEmail(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Sucesso = false;
                ViewBag.Mensagem = "O link de validação está incompleto.";
                return View();
            }

            try
            {
                var (sucesso, mensagem) = await _api.ValidarEmailAsync(token);
                ViewBag.Sucesso = sucesso;
                ViewBag.Mensagem = mensagem;
                return View();
            }
            catch (Exception)
            {
                ViewBag.Sucesso = false;
                ViewBag.Mensagem = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
                return View();
            }
        }

        // GET: /Conta/ReenviarValidacao
        [HttpGet]
        public IActionResult ReenviarValidacao()
        {
            return View(new RecuperarPasswordViewModel());
        }

        // POST: /Conta/ReenviarValidacao
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarValidacao(RecuperarPasswordViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            try
            {
                var (resposta, erro) = await _api.ReenviarValidacaoAsync(modelo.Email);

                if (resposta == null)
                {
                    ModelState.AddModelError(string.Empty, erro ?? "Não foi possível gerar o link.");
                    return View(modelo);
                }

                TempData["Sucesso"] = resposta.Mensagem;
                GuardarLinkDev(resposta.Token, "ValidarEmail", "Link de validação de email");

                return RedirectToAction(nameof(Login));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,
                    "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.");
                return View(modelo);
            }
        }

        // ------------------------------------------------------------
        // Recuperação de password
        // ------------------------------------------------------------

        // GET: /Conta/RecuperarPassword
        [HttpGet]
        public IActionResult RecuperarPassword()
        {
            return View(new RecuperarPasswordViewModel());
        }

        // POST: /Conta/RecuperarPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarPassword(RecuperarPasswordViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            try
            {
                var (resposta, erro) = await _api.RecuperarPasswordAsync(modelo.Email);

                if (resposta == null)
                {
                    ModelState.AddModelError(string.Empty, erro ?? "Não foi possível gerar o link.");
                    return View(modelo);
                }

                TempData["Sucesso"] = resposta.Mensagem;
                GuardarLinkDev(resposta.Token, "ReporPassword", "Link de recuperação de password");

                return RedirectToAction(nameof(Login));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,
                    "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.");
                return View(modelo);
            }
        }

        // GET: /Conta/ReporPassword?token=RP....  (o "link do email")
        [HttpGet]
        public IActionResult ReporPassword(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Erro"] = "O link de recuperação está incompleto.";
                return RedirectToAction(nameof(RecuperarPassword));
            }

            return View(new ReporPasswordViewModel { Token = token });
        }

        // POST: /Conta/ReporPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReporPassword(ReporPasswordViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            try
            {
                var (sucesso, mensagem) = await _api.ReporPasswordAsync(modelo.Token, modelo.NovaPassword);

                if (!sucesso)
                {
                    ModelState.AddModelError(string.Empty, mensagem);
                    return View(modelo);
                }

                TempData["Sucesso"] = mensagem;
                return RedirectToAction(nameof(Login));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,
                    "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.");
                return View(modelo);
            }
        }

        // ------------------------------------------------------------
        // Alteração de password (apenas com sessão iniciada)
        // ------------------------------------------------------------

        // GET: /Conta/AlterarPassword
        [Authorize]
        [HttpGet]
        public IActionResult AlterarPassword()
        {
            return View(new AlterarPasswordViewModel());
        }

        // POST: /Conta/AlterarPassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarPassword(AlterarPasswordViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            try
            {
                var (sucesso, mensagem) = await _api.AlterarPasswordAsync(
                    ObterUtilizadorId(), modelo.PasswordAtual, modelo.NovaPassword);

                if (!sucesso)
                {
                    ModelState.AddModelError(string.Empty, mensagem);
                    return View(modelo);
                }

                TempData["Sucesso"] = mensagem;
                return RedirectToAction(nameof(Perfil));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,
                    "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.");
                return View(modelo);
            }
        }

        // ------------------------------------------------------------
        // Perfil (apenas com sessão iniciada)
        // ------------------------------------------------------------

        // GET: /Conta/Perfil
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            try
            {
                var utilizador = await _api.ObterUtilizadorAsync(ObterUtilizadorId());

                if (utilizador == null)
                {
                    TempData["Erro"] = "Não foi possível obter os dados do perfil.";
                    return RedirectToAction("Index", "Home");
                }

                return View(new PerfilViewModel
                {
                    NomeUtilizador = utilizador.NomeUtilizador,
                    FotoPerfilUrl = utilizador.FotoPerfilUrl,
                    Email = utilizador.Email,
                    DataCriacao = utilizador.DataCriacao,
                    UltimoLogin = utilizador.UltimoLogin,
                    IsAdmin = utilizador.IsAdmin
                });
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Conta/Perfil  (UPDATE via API)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(PerfilViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                await PreencherCamposLeituraAsync(modelo);
                return View(modelo);
            }

            try
            {
                var (utilizador, erro) = await _api.AtualizarPerfilAsync(ObterUtilizadorId(), modelo);

                if (utilizador == null)
                {
                    ModelState.AddModelError(string.Empty, erro ?? "Não foi possível atualizar o perfil.");
                    await PreencherCamposLeituraAsync(modelo);
                    return View(modelo);
                }

                // Atualiza o cookie para o novo nome aparecer já no cabeçalho,
                // mantendo a persistência escolhida no login ("manter sessão")
                var autenticacao = await HttpContext.AuthenticateAsync();
                await IniciarSessaoAsync(utilizador, autenticacao.Properties?.IsPersistent ?? false);

                TempData["Sucesso"] = "Perfil atualizado com sucesso.";
                return RedirectToAction(nameof(Perfil));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Conta/AcessoNegado  (utilizador autenticado sem permissões, ex.: Gestão)
        [HttpGet]
        public IActionResult AcessoNegado()
        {
            return View();
        }

        // ============================================================
        //  Métodos auxiliares
        // ============================================================

        /// <summary>
        /// Volta a preencher os campos apenas de leitura do perfil (email,
        /// datas) quando o formulário é devolvido com erros de validação.
        /// </summary>
        private async Task PreencherCamposLeituraAsync(PerfilViewModel modelo)
        {
            try
            {
                var utilizador = await _api.ObterUtilizadorAsync(ObterUtilizadorId());
                if (utilizador != null)
                {
                    modelo.Email = utilizador.Email;
                    modelo.DataCriacao = utilizador.DataCriacao;
                    modelo.UltimoLogin = utilizador.UltimoLogin;
                    modelo.IsAdmin = utilizador.IsAdmin;
                }
            }
            catch (Exception)
            {
                // Sem API disponível, a página mostra os campos de leitura vazios
            }
        }

        /// <summary>Id do utilizador autenticado (guardado no cookie).</summary>
        private int ObterUtilizadorId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }

        /// <summary>Cria o cookie de autenticação com os dados do utilizador.</summary>
        private async Task IniciarSessaoAsync(UtilizadorSessao utilizador, bool lembrar)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, utilizador.Id.ToString()),
                new Claim(ClaimTypes.Name, utilizador.NomeUtilizador),
                new Claim(ClaimTypes.Email, utilizador.Email)
            };

            // O papel (role) "Admin" dá acesso à área de gestão
            if (utilizador.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            if (!string.IsNullOrEmpty(utilizador.FotoPerfilUrl))
            {
                claims.Add(new Claim("FotoPerfilUrl", utilizador.FotoPerfilUrl));
            }

            var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidade),
                new AuthenticationProperties
                {
                    // "Manter sessão iniciada": o cookie sobrevive ao fecho do browser
                    IsPersistent = lembrar,
                    ExpiresUtc = lembrar ? DateTimeOffset.UtcNow.AddDays(14) : null
                });
        }

        /// <summary>
        /// Guarda o link "de email" no TempData para ser mostrado no ecrã
        /// (modo de desenvolvimento, sem envio real de emails).
        /// </summary>
        private void GuardarLinkDev(string? token, string acao, string titulo)
        {
            if (string.IsNullOrEmpty(token))
            {
                return; // resposta neutra da API (email não existe) - nada a mostrar
            }

            string? link = Url.Action(acao, "Conta", new { token }, Request.Scheme);
            TempData["LinkDevTitulo"] = titulo;
            TempData["LinkDev"] = link;
        }
    }
}
