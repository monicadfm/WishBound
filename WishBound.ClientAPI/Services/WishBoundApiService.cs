using System.Net;
using System.Net.Http.Json;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Models.Colecao;
using WishBound.ClientAPI.Models.Conta;

namespace WishBound.ClientAPI.Services
{
    /// <summary>
    /// Serviço responsável por toda a comunicação com a WishBound.WebAPI.
    /// Os controllers usam este serviço em vez de falarem diretamente com o HttpClient.
    /// Os erros de rede são tratados (try-catch) nos controllers.
    /// </summary>
    public class WishBoundApiService
    {
        private readonly HttpClient _http;

        public WishBoundApiService(HttpClient http)
        {
            _http = http;
        }

        // ---------- Personagens ----------

        /// <summary>SELECT - obtém todas as personagens.</summary>
        public async Task<List<Personagem>> ObterPersonagensAsync()
        {
            return await _http.GetFromJsonAsync<List<Personagem>>("api/personagens")
                   ?? new List<Personagem>();
        }

        /// <summary>SELECT - obtém uma personagem pelo Id (null se não existir).</summary>
        public async Task<Personagem?> ObterPersonagemAsync(int id)
        {
            var resposta = await _http.GetAsync("api/personagens/" + id);

            if (resposta.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            resposta.EnsureSuccessStatusCode();
            return await resposta.Content.ReadFromJsonAsync<Personagem>();
        }

        /// <summary>INSERT - cria uma nova personagem.</summary>
        public async Task<(bool Sucesso, string? Erro)> CriarPersonagemAsync(Personagem personagem)
        {
            var resposta = await _http.PostAsJsonAsync("api/personagens", personagem);

            if (resposta.IsSuccessStatusCode)
            {
                return (true, null);
            }

            string erro = await resposta.Content.ReadAsStringAsync();
            return (false, erro);
        }

        /// <summary>UPDATE - atualiza uma personagem existente.</summary>
        public async Task<(bool Sucesso, string? Erro)> AtualizarPersonagemAsync(Personagem personagem)
        {
            var resposta = await _http.PutAsJsonAsync("api/personagens/" + personagem.Id, personagem);

            if (resposta.IsSuccessStatusCode)
            {
                return (true, null);
            }

            string erro = await resposta.Content.ReadAsStringAsync();
            return (false, erro);
        }

        /// <summary>DELETE - apaga uma personagem.</summary>
        public async Task<(bool Sucesso, string? Erro)> ApagarPersonagemAsync(int id)
        {
            var resposta = await _http.DeleteAsync("api/personagens/" + id);

            if (resposta.IsSuccessStatusCode)
            {
                return (true, null);
            }

            string erro = await resposta.Content.ReadAsStringAsync();
            return (false, erro);
        }

        // ---------- Raridades ----------

        /// <summary>SELECT - obtém todas as raridades (para dropdowns e probabilidades).</summary>
        public async Task<List<Raridade>> ObterRaridadesAsync()
        {
            return await _http.GetFromJsonAsync<List<Raridade>>("api/raridades")
                   ?? new List<Raridade>();
        }

        // ---------- Invocações (gacha) ----------

        /// <summary>
        /// Realiza uma invocação em nome do utilizador autenticado. Devolve o
        /// resultado (personagem, se é nova ou repetida, espaço ocupado) ou a
        /// mensagem de erro da API — por exemplo quando a coleção está cheia.
        /// </summary>
        public async Task<(ResultadoInvocacao? Resultado, string? Erro)> InvocarAsync(int utilizadorId)
        {
            var resposta = await _http.PostAsJsonAsync("api/invocacoes", new
            {
                UtilizadorId = utilizadorId
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<ResultadoInvocacao>(), null);
        }

        /// <summary>SELECT - obtém o histórico de invocações DO utilizador.</summary>
        public async Task<List<Invocacao>> ObterHistoricoAsync(int utilizadorId)
        {
            return await _http.GetFromJsonAsync<List<Invocacao>>("api/invocacoes?utilizadorId=" + utilizadorId)
                   ?? new List<Invocacao>();
        }

        // ---------- Coleção pessoal ----------

        /// <summary>SELECT - coleção do utilizador, já ordenada pela API.</summary>
        public async Task<ColecaoViewModel> ObterColecaoAsync(int utilizadorId, string ordenar, bool apenasFavoritos)
        {
            string url = "api/colecao?utilizadorId=" + utilizadorId +
                         "&ordenar=" + Uri.EscapeDataString(ordenar) +
                         "&favoritos=" + (apenasFavoritos ? "true" : "false");

            return await _http.GetFromJsonAsync<ColecaoViewModel>(url) ?? new ColecaoViewModel();
        }

        /// <summary>SELECT - uma personagem da coleção (null se não a tiver).</summary>
        public async Task<ItemColecao?> ObterItemColecaoAsync(int utilizadorId, int personagemId)
        {
            var resposta = await _http.GetAsync(
                "api/colecao/item?utilizadorId=" + utilizadorId + "&personagemId=" + personagemId);

            // Só o 404 significa "não tem esta personagem"; qualquer outro erro
            // (API em baixo, chave errada) deve rebentar para o controller
            // mostrar a mensagem certa.
            if (resposta.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            resposta.EnsureSuccessStatusCode();
            return await resposta.Content.ReadFromJsonAsync<ItemColecao>();
        }

        /// <summary>UPDATE - marca ou desmarca uma personagem como favorita.</summary>
        public async Task<(bool Sucesso, string Mensagem)> MarcarFavoritoAsync(int utilizadorId, int personagemId, bool favorito)
        {
            var resposta = await _http.PostAsJsonAsync("api/colecao/favorito", new
            {
                UtilizadorId = utilizadorId,
                PersonagemId = personagemId,
                Favorito = favorito
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>UPDATE - liberta cópias repetidas (mantém sempre uma).</summary>
        public async Task<(bool Sucesso, string Mensagem)> LibertarDuplicadosAsync(int utilizadorId, int personagemId, int quantidade)
        {
            var resposta = await _http.PostAsJsonAsync("api/colecao/libertar", new
            {
                UtilizadorId = utilizadorId,
                PersonagemId = personagemId,
                Quantidade = quantidade
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>UPDATE - compra mais lugares para a coleção, pagos em Moedas.</summary>
        public async Task<(bool Sucesso, string Mensagem)> ExpandirInventarioAsync(int utilizadorId)
        {
            var resposta = await _http.PostAsJsonAsync("api/colecao/expandir", new
            {
                UtilizadorId = utilizadorId
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        // ---------- Conta e autenticação ----------

        /// <summary>Regista um novo utilizador. Devolve a resposta com o token de validação (modo dev).</summary>
        public async Task<(TokenResposta? Resposta, string? Erro)> RegistarAsync(RegistoViewModel registo)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/registar", new
            {
                registo.NomeUtilizador,
                registo.Email,
                registo.Password
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<TokenResposta>(), null);
        }

        /// <summary>Verifica as credenciais. Devolve o utilizador se o login for válido.</summary>
        public async Task<(UtilizadorSessao? Utilizador, string? Erro)> LoginAsync(string identificador, string password)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/login", new
            {
                Identificador = identificador,
                Password = password
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<UtilizadorSessao>(), null);
        }

        /// <summary>
        /// Login com Google: a API liga/cria a conta associada a esta conta
        /// Google e devolve o utilizador (chamado APÓS a Google confirmar a
        /// identidade no fluxo OAuth).
        /// </summary>
        public async Task<(UtilizadorSessao? Utilizador, string? Erro)> LoginGoogleAsync(
            string googleId, string email, string? nome, string? fotoUrl)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/login-google", new
            {
                GoogleId = googleId,
                Email = email,
                Nome = nome,
                FotoUrl = fotoUrl
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<UtilizadorSessao>(), null);
        }

        /// <summary>Valida o email de uma conta com o token do link.</summary>
        public async Task<(bool Sucesso, string Mensagem)> ValidarEmailAsync(string token)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/validar-email", new { Token = token });
            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>Pede um novo link de validação de email.</summary>
        public async Task<(TokenResposta? Resposta, string? Erro)> ReenviarValidacaoAsync(string email)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/reenviar-validacao", new { Email = email });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<TokenResposta>(), null);
        }

        /// <summary>Pede um link de recuperação de password.</summary>
        public async Task<(TokenResposta? Resposta, string? Erro)> RecuperarPasswordAsync(string email)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/recuperar-password", new { Email = email });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<TokenResposta>(), null);
        }

        /// <summary>Repõe a password usando o token do link de recuperação.</summary>
        public async Task<(bool Sucesso, string Mensagem)> ReporPasswordAsync(string token, string novaPassword)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/repor-password", new
            {
                Token = token,
                NovaPassword = novaPassword
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Altera (ou define, nas contas Google sem password local) a password
        /// de um utilizador autenticado. passwordAtual pode ir a null quando a
        /// conta ainda não tem password — a API confirma esse caso.
        /// </summary>
        public async Task<(bool Sucesso, string Mensagem)> AlterarPasswordAsync(int utilizadorId, string? passwordAtual, string novaPassword)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/alterar-password", new
            {
                UtilizadorId = utilizadorId,
                PasswordAtual = passwordAtual,
                NovaPassword = novaPassword
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>SELECT - obtém os dados públicos de um utilizador.</summary>
        public async Task<UtilizadorSessao?> ObterUtilizadorAsync(int id)
        {
            var resposta = await _http.GetAsync("api/conta/" + id);

            if (!resposta.IsSuccessStatusCode)
            {
                return null;
            }

            return await resposta.Content.ReadFromJsonAsync<UtilizadorSessao>();
        }

        /// <summary>UPDATE - atualiza o perfil e devolve os dados atualizados.</summary>
        public async Task<(UtilizadorSessao? Utilizador, string? Erro)> AtualizarPerfilAsync(int utilizadorId, PerfilViewModel perfil)
        {
            var resposta = await _http.PutAsJsonAsync("api/conta/perfil", new
            {
                UtilizadorId = utilizadorId,
                perfil.NomeUtilizador,
                perfil.FotoPerfilUrl
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<UtilizadorSessao>(), null);
        }
    }
}
