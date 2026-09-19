using WishBound.Mobile.Models;
using WishBound.Mobile.ViewModels;

namespace WishBound.Mobile.Views
{
    public partial class ColecaoPage : ContentPage
    {
        private readonly ColecaoViewModel _viewModel;

        public ColecaoPage(ColecaoViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        // Recarrega sempre que a aba aparece (ex.: depois de marcar favorita nos detalhes)
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CarregarAsync();
        }

        // Toque no cartão: abre os detalhes da personagem
        private async void AoTocarPersonagem(object? sender, TappedEventArgs e)
        {
            if (sender is BindableObject cartao && cartao.BindingContext is ItemColecao item)
            {
                await Shell.Current.GoToAsync(nameof(DetalhesPersonagemPage) + "?personagemId=" + item.PersonagemId);
            }
        }

        // Toque na estrela: marca/desmarca favorita sem sair da lista
        private async void AoTocarEstrela(object? sender, EventArgs e)
        {
            if (sender is BindableObject botao && botao.BindingContext is ItemColecao item)
            {
                await _viewModel.AlternarFavoritoAsync(item);
            }
        }
    }
}
