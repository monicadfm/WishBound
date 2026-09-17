using WishBound.Mobile.ViewModels;

namespace WishBound.Mobile.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginViewModel _viewModel;

        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Só navega depois de a página estar mesmo no ecrã
            Dispatcher.Dispatch(async () => await _viewModel.VerificarSessaoAsync());
        }
    }
}
