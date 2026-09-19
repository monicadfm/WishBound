using WishBound.Mobile.Views;

namespace WishBound.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Páginas que se abrem "por cima" de uma aba (não são abas):
            // Shell.Current.GoToAsync(nameof(DetalhesPersonagemPage) + "?personagemId=3")
            Routing.RegisterRoute(nameof(DetalhesPersonagemPage), typeof(DetalhesPersonagemPage));
        }
    }
}
