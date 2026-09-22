using WishBound.Mobile.Models;
using WishBound.Mobile.ViewModels;

namespace WishBound.Mobile.Views
{
    public partial class InicioPage : ContentPage
    {
        private readonly InicioViewModel _viewModel;

        public InicioPage(InicioViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        // Sempre que a página aparece vai buscar a companheira e uma saudação nova
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CarregarAsync();
        }

        // Toque no sino: muda para a aba Notificações
        private async void AoTocarNotificacoes(object? sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("//notificacoes");
        }

        // Toque numa linha da lista: o BindingContext da linha é a candidata
        private async void AoTocarCandidata(object? sender, TappedEventArgs e)
        {
            if (sender is BindableObject linha && linha.BindingContext is CandidataCompanheira candidata)
            {
                await _viewModel.EscolherAsync(candidata);
            }
        }
    }
}
