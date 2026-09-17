using System.Text.Json;
using WishBound.Mobile.Models;

namespace WishBound.Mobile.Services
{
    // ============================================================
    //  Sessão do utilizador no telemóvel.
    //  Fica guardada no SecureStorage (armazenamento cifrado do
    //  sistema operativo), para não ser preciso fazer login sempre
    //  que a aplicação abre. Nunca se guarda a password.
    // ============================================================
    public class ServicoSessao
    {
        private const string ChaveSessao = "wishbound_sessao";

        public UtilizadorSessao? Atual { get; private set; }

        public bool TemSessao => Atual != null;

        public async Task GuardarAsync(UtilizadorSessao utilizador)
        {
            Atual = utilizador;
            var json = JsonSerializer.Serialize(utilizador);

            try
            {
                await SecureStorage.Default.SetAsync(ChaveSessao, json);
            }
            catch (Exception)
            {
                // Alguns dispositivos/emuladores não têm SecureStorage disponível:
                // nesse caso usamos as preferências normais.
                Preferences.Default.Set(ChaveSessao, json);
            }
        }

        /// <summary>Lê a sessão guardada (se existir). Devolve true se havia sessão.</summary>
        public async Task<bool> RestaurarAsync()
        {
            if (Atual != null)
            {
                return true;
            }

            string? json = null;

            try
            {
                json = await SecureStorage.Default.GetAsync(ChaveSessao);
            }
            catch (Exception)
            {
                // ignorar - tenta-se as preferências a seguir
            }

            if (string.IsNullOrEmpty(json))
            {
                json = Preferences.Default.Get(ChaveSessao, string.Empty);
            }

            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                Atual = JsonSerializer.Deserialize<UtilizadorSessao>(json);
            }
            catch (JsonException)
            {
                Atual = null;
            }

            return Atual != null;
        }

        public void Terminar()
        {
            Atual = null;

            try
            {
                SecureStorage.Default.Remove(ChaveSessao);
            }
            catch (Exception)
            {
                // ignorar
            }

            Preferences.Default.Remove(ChaveSessao);
        }
    }
}
