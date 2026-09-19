using System.Net.Http.Json;
using System.Text.Json;
using WishBound.Mobile.Models;

namespace WishBound.Mobile.Services
{
    // ============================================================
    //  Todas as chamadas à WishBound.WebAPI passam por aqui.
    //  Tal como o site (ClientAPI), a app nunca toca na base de
    //  dados: fala só com a API, por HttpClient, e envia em todos
    //  os pedidos o cabeçalho X-WishBound-Chave.
    // ============================================================
    public class ServicoApi
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions OpcoesJson = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public ServicoApi()
        {
            _http = new HttpClient
            {
                // 30 s: o primeiro pedido depois de arrancar a API é lento
                // (ligação ao SQL Server + modelo do EF + hash PBKDF2 da password)
                Timeout = TimeSpan.FromSeconds(30)
            };
            _http.DefaultRequestHeaders.Add(Configuracao.CabecalhoChaveApi, Configuracao.ChaveApi);
        }

        // ----- Conta -----

        public Task<ResultadoApi<UtilizadorSessao>> LoginAsync(string identificador, string password)
        {
            var pedido = new LoginPedido { Identificador = identificador, Password = password };
            return EnviarAsync<UtilizadorSessao>(HttpMethod.Post, "api/conta/login", pedido);
        }

        // ----- Companheira (página inicial) -----

        public Task<ResultadoApi<CompanheiraResposta>> ObterCompanheiraAsync(int utilizadorId)
        {
            return EnviarAsync<CompanheiraResposta>(HttpMethod.Get, "api/mensagens/companheira?utilizadorId=" + utilizadorId, null);
        }

        /// <summary>A API responde com uma frase (texto), por isso o resultado é string.</summary>
        public Task<ResultadoApi<string>> EscolherCompanheiraAsync(int utilizadorId, int? personagemId)
        {
            var pedido = new EscolherCompanheiraPedido { UtilizadorId = utilizadorId, PersonagemId = personagemId };
            return EnviarAsync<string>(HttpMethod.Post, "api/mensagens/companheira", pedido);
        }

        // ----- Coleção -----

        public Task<ResultadoApi<ColecaoResposta>> ObterColecaoAsync(int utilizadorId, string ordenar, bool favoritos)
        {
            return EnviarAsync<ColecaoResposta>(HttpMethod.Get,
                "api/colecao?utilizadorId=" + utilizadorId + "&ordenar=" + ordenar + "&favoritos=" + (favoritos ? "true" : "false"), null);
        }

        public Task<ResultadoApi<ItemColecao>> ObterItemColecaoAsync(int utilizadorId, int personagemId)
        {
            return EnviarAsync<ItemColecao>(HttpMethod.Get,
                "api/colecao/item?utilizadorId=" + utilizadorId + "&personagemId=" + personagemId, null);
        }

        public Task<ResultadoApi<string>> MarcarFavoritoAsync(int utilizadorId, int personagemId, bool favorito)
        {
            var pedido = new FavoritoPedido { UtilizadorId = utilizadorId, PersonagemId = personagemId, Favorito = favorito };
            return EnviarAsync<string>(HttpMethod.Post, "api/colecao/favorito", pedido);
        }

        // ----- Amizade -----

        public Task<ResultadoApi<InteracaoResposta>> InteragirAsync(int utilizadorId, int personagemId)
        {
            var pedido = new InteragirPedido { UtilizadorId = utilizadorId, PersonagemId = personagemId };
            return EnviarAsync<InteracaoResposta>(HttpMethod.Post, "api/amizade/interagir", pedido);
        }

        // ----- Mensagens de uma personagem -----

        public Task<ResultadoApi<MensagensPersonagemResposta>> ObterMensagensPersonagemAsync(int utilizadorId, int personagemId)
        {
            return EnviarAsync<MensagensPersonagemResposta>(HttpMethod.Get,
                "api/mensagens/personagem?utilizadorId=" + utilizadorId + "&personagemId=" + personagemId, null);
        }

        // ------------------------------------------------------------
        //  Método comum: envia o pedido, lê a resposta e transforma
        //  qualquer problema (rede, 401, 500...) numa mensagem legível.
        // ------------------------------------------------------------
        private async Task<ResultadoApi<T>> EnviarAsync<T>(HttpMethod metodo, string caminho, object? corpo)
        {
            var url = Configuracao.UrlApi + "/" + caminho;

            try
            {
                using var pedido = new HttpRequestMessage(metodo, url);

                if (corpo != null)
                {
                    pedido.Content = JsonContent.Create(corpo, corpo.GetType(), options: OpcoesJson);
                }

                using var resposta = await _http.SendAsync(pedido);
                var texto = await resposta.Content.ReadAsStringAsync();

                if (!resposta.IsSuccessStatusCode)
                {
                    return ResultadoApi<T>.Falha(LerErro(texto, (int)resposta.StatusCode));
                }

                // Respostas que são só uma frase (ex.: "Luna passa a receber-te...")
                if (typeof(T) == typeof(string))
                {
                    return ResultadoApi<T>.Ok((T)(object)LimparTexto(texto));
                }

                var dados = JsonSerializer.Deserialize<T>(texto, OpcoesJson);
                return ResultadoApi<T>.Ok(dados);
            }
            catch (HttpRequestException)
            {
                return ResultadoApi<T>.Falha("Não foi possível ligar à API em " + Configuracao.UrlApi + ". Confirma que a WishBound.WebAPI está a correr.");
            }
            catch (TaskCanceledException)
            {
                return ResultadoApi<T>.Falha("A API em " + Configuracao.UrlApi + " demorou demasiado a responder.");
            }
            catch (UriFormatException)
            {
                return ResultadoApi<T>.Falha("O endereço da API não é válido: " + Configuracao.UrlApi);
            }
            catch (InvalidOperationException)
            {
                return ResultadoApi<T>.Falha("O endereço da API não é válido: " + Configuracao.UrlApi);
            }
            catch (JsonException)
            {
                return ResultadoApi<T>.Falha("A API devolveu uma resposta inesperada.");
            }
        }

        /// <summary>
        /// A API devolve os erros como texto simples ("Credenciais inválidas.").
        /// Os erros de validação automática vêm em JSON - nesse caso mostra-se
        /// uma mensagem genérica.
        /// </summary>
        private static string LerErro(string texto, int codigo)
        {
            var limpo = LimparTexto(texto);

            if (string.IsNullOrWhiteSpace(limpo))
            {
                return codigo == 401
                    ? "Pedido recusado pela API (chave de API incorreta?)."
                    : "A API respondeu com o erro " + codigo + ".";
            }

            if (limpo.StartsWith("{") || limpo.StartsWith("<"))
            {
                return "Os dados enviados não são válidos (erro " + codigo + ").";
            }

            return limpo;
        }

        private static string LimparTexto(string texto)
        {
            return (texto ?? string.Empty).Trim().Trim('"');
        }
    }
}
