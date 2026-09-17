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
