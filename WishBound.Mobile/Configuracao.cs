namespace WishBound.Mobile
{
    // ============================================================
    //  Configuração da ligação à WebAPI.
    //
    //  - Emulador Android: o PC onde corre a API chama-se 10.0.2.2
    //    (o "localhost" do emulador é o próprio emulador).
    //  - Windows: a API está no mesmo PC -> localhost.
    //  - Telemóvel a sério (mesma rede Wi-Fi): usar o IP do PC, por
    //    exemplo http://192.168.1.50:5240 - muda-se no ecrã de login,
    //    em "Ligação à API", e fica guardado nas preferências.
    // ============================================================
    public static class Configuracao
    {
        private const string ChavePreferenciaUrl = "UrlApi";

        /// <summary>Tem de ser igual a Seguranca:ChaveApi da WebAPI.</summary>
        public const string ChaveApi = "wishbound-dev-2026";

        /// <summary>Nome do cabeçalho verificado pelo ChaveApiMiddleware.</summary>
        public const string CabecalhoChaveApi = "X-WishBound-Chave";

        public static string UrlApiPorOmissao =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5240"
                : "http://localhost:5240";

        /// <summary>
        /// Endereço do SITE (WishBound.ClientAPI), de onde vêm as imagens das
        /// personagens (/img/personagens/luna.svg). Deriva do endereço da API:
        /// mesmo PC, porta 5100 em vez de 5240.
        /// </summary>
        public static string UrlSite => UrlApi.Replace(":5240", ":5100");

        /// <summary>
        /// Nome do sprite embutido na app para uma personagem:
        /// "/img/personagens/luna.svg" -> "luna.png" (Resources/Images/personagens).
        /// Se a personagem não tiver sprite na app, o Image fica vazio e vê-se a inicial.
        /// </summary>
        public static string? NomeSprite(string? imagemUrl)
        {
            if (string.IsNullOrWhiteSpace(imagemUrl))
            {
                return null;
            }

            var nome = Path.GetFileNameWithoutExtension(imagemUrl).Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(nome) ? null : nome + ".png";
        }

        /// <summary>Transforma "/img/personagens/luna.svg" num URL completo do site.</summary>
        public static string? UrlImagem(string? caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho))
            {
                return null;
            }

            if (caminho.StartsWith("http://") || caminho.StartsWith("https://"))
            {
                return caminho;
            }

            return UrlSite + (caminho.StartsWith("/") ? caminho : "/" + caminho);
        }

        /// <summary>Endereço base da API, sem barra final.</summary>
        public static string UrlApi
        {
            get
            {
                var guardado = Preferences.Default.Get(ChavePreferenciaUrl, string.Empty);
                return string.IsNullOrWhiteSpace(guardado) ? UrlApiPorOmissao : guardado;
            }
            set
            {
                var limpo = (value ?? string.Empty).Trim().TrimEnd('/');

                if (string.IsNullOrEmpty(limpo) || limpo == UrlApiPorOmissao)
                {
                    Preferences.Default.Remove(ChavePreferenciaUrl);
                }
                else
                {
                    Preferences.Default.Set(ChavePreferenciaUrl, limpo);
                }
            }
        }
    }
}
