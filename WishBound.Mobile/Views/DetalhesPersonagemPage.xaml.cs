using WishBound.Mobile.ViewModels;

namespace WishBound.Mobile.Views
{
    // O Id chega pelo URL de navegação: DetalhesPersonagemPage?personagemId=3
    [QueryProperty(nameof(PersonagemId), "personagemId")]
    public partial class DetalhesPersonagemPage : ContentPage
    {
        private readonly DetalhesPersonagemViewModel _viewModel;

        public DetalhesPersonagemPage(DetalhesPersonagemViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        private string _personagemId = string.Empty;

        public string PersonagemId
        {
            get => _personagemId;
            set
            {
                _personagemId = value;

                if (int.TryParse(value, out var id))
                {
                    _viewModel.PersonagemId = id;
                }
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CarregarAsync();
        }
    }
}
