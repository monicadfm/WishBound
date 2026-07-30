using System.Net;
using System.Net.Http.Json;
using WishBound.ClientAPI.Models;

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

        /// <summary>Realiza uma invocação e devolve a personagem obtida.</summary>
        public async Task<Personagem?> InvocarAsync()
        {
            var resposta = await _http.PostAsync("api/invocacoes", null);

            if (!resposta.IsSuccessStatusCode)
            {
                return null;
            }

            return await resposta.Content.ReadFromJsonAsync<Personagem>();
        }

        /// <summary>SELECT - obtém o histórico de invocações.</summary>
        public async Task<List<Invocacao>> ObterHistoricoAsync()
        {
            return await _http.GetFromJsonAsync<List<Invocacao>>("api/invocacoes")
                   ?? new List<Invocacao>();
        }
    }
}
