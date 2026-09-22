using WishBound.Mobile.Models;
using WishBound.Mobile.ViewModels;

namespace WishBound.Mobile.Views
{
    public partial class NotificacoesPage : ContentPage
    {
        private readonly NotificacoesViewModel _viewModel;

        public NotificacoesPage(NotificacoesViewModel viewModel)
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

        // Toque numa notificação: marca-a como lida
        private async void AoTocarNotificacao(object? sender, TappedEventArgs e)
        {
            if (sender is BindableObject cartao && cartao.BindingContext is Notificacao notificacao)
            {
                await _viewModel.MarcarLidaAsync(notificacao);
            }
        }
    }
}
