using WishBound.Mobile.Models;
using WishBound.Mobile.ViewModels;

namespace WishBound.Mobile.Views
{
    public partial class RecompensaPage : ContentPage
    {
        private readonly RecompensaViewModel _viewModel;

        public RecompensaPage(RecompensaViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CarregarAsync();
        }

        // Botão de um evento: o BindingContext do botão é o próprio evento
        private async void AoResgatarEvento(object? sender, EventArgs e)
        {
            if (sender is BindableObject botao && botao.BindingContext is EventoResposta evento)
            {
                await _viewModel.ResgatarEventoAsync(evento);
            }
        }
    }
}
