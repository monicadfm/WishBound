using WishBound.Mobile.Services;
using WishBound.Mobile.ViewModels;
using WishBound.Mobile.Views;

namespace WishBound.Mobile
{
    // ============================================================
    //  Ponto de arranque da aplicação MAUI.
    //  Aqui registam-se os serviços, os view models e as páginas
    //  (injeção de dependências, tal como no Program.cs da WebAPI).
    // ============================================================
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder.UseMauiApp<App>();

            // Serviços: uma única instância para toda a aplicação
            builder.Services.AddSingleton<ServicoSessao>();
            builder.Services.AddSingleton<ServicoApi>();

            // View models + páginas
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();

            builder.Services.AddTransient<InicioViewModel>();
            builder.Services.AddTransient<InicioPage>();

            return builder.Build();
        }
    }
}
