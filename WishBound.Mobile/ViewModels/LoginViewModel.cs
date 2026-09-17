using System.Windows.Input;
using WishBound.Mobile.Services;

namespace WishBound.Mobile.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly ServicoApi _api;
        private readonly ServicoSessao _sessao;

        private string _identificador = string.Empty;
        private string _password = string.Empty;
        private string _erro = string.Empty;
        private string _urlApi = Configuracao.UrlApi;
        private bool _mostrarLigacao;

        public LoginViewModel(ServicoApi api, ServicoSessao sessao)
        {
            _api = api;
            _sessao = sessao;

            EntrarCommand = new Command(async () => await EntrarAsync());
            AlternarLigacaoCommand = new Command(() => MostrarLigacao = !MostrarLigacao);
            ReporUrlCommand = new Command(() => UrlApi = Configuracao.UrlApiPorOmissao);
        }

        public ICommand EntrarCommand { get; }
        public ICommand AlternarLigacaoCommand { get; }
        public ICommand ReporUrlCommand { get; }

        /// <summary>Nome de utilizador OU email (a API aceita os dois).</summary>
        public string Identificador
        {
            get => _identificador;
            set => Definir(ref _identificador, value);
        }

        public string Password
        {
            get => _password;
            set => Definir(ref _password, value);
        }

        public string Erro
        {
            get => _erro;
            set
            {
                if (Definir(ref _erro, value))
                {
                    Notificar(nameof(TemErro));
                }
            }
        }

        public bool TemErro => !string.IsNullOrEmpty(_erro);

        /// <summary>Endereço da WebAPI (editável no painel "Ligação à API").</summary>
        public string UrlApi
        {
            get => _urlApi;
            set => Definir(ref _urlApi, value);
        }

        public bool MostrarLigacao
        {
            get => _mostrarLigacao;
            set => Definir(ref _mostrarLigacao, value);
        }

        /// <summary>Chamado quando a página aparece: se já havia sessão guardada, salta o login.</summary>
        public async Task VerificarSessaoAsync()
        {
            if (await _sessao.RestaurarAsync())
            {
                await Shell.Current.GoToAsync("//inicio");
            }
        }

        private async Task EntrarAsync()
        {
            if (Ocupado)
            {
                return;
            }

            Erro = string.Empty;

            if (string.IsNullOrWhiteSpace(Identificador) || string.IsNullOrEmpty(Password))
            {
                Erro = "Indica o nome de utilizador (ou email) e a password.";
                return;
            }

            Ocupado = true;

            try
            {
                Configuracao.UrlApi = UrlApi;
                UrlApi = Configuracao.UrlApi;

                var resultado = await _api.LoginAsync(Identificador.Trim(), Password);

                if (!resultado.Sucesso || resultado.Dados == null)
                {
                    Erro = string.IsNullOrEmpty(resultado.Erro) ? "Não foi possível iniciar sessão." : resultado.Erro;
                    return;
                }

                await _sessao.GuardarAsync(resultado.Dados);

                Password = string.Empty;
                await Shell.Current.GoToAsync("//inicio");
            }
            finally
            {
                Ocupado = false;
            }
        }
    }
}
