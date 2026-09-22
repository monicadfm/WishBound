using System.Collections.ObjectModel;
using System.Windows.Input;
using WishBound.Mobile.Models;
using WishBound.Mobile.Services;

namespace WishBound.Mobile.ViewModels
{
    /// <summary>
    /// Página inicial: a companheira escolhida recebe o utilizador com uma
    /// saudação. É a MESMA companheira do site (Utilizadores.PersonagemCompanheiraId),
    /// por isso trocar aqui muda também a página inicial do site.
    /// </summary>
    public class InicioViewModel : BaseViewModel
    {
        private readonly ServicoApi _api;
        private readonly ServicoSessao _sessao;

        private string _boasVindas = string.Empty;
        private bool _ehAdmin;
        private CandidataCompanheira? _companheira;
        private string _saudacao = string.Empty;
        private string _mensagem = string.Empty;
        private bool _mostrarCandidatas;
        private bool _carregado;
        private string _textoNotificacoes = string.Empty;

        public InicioViewModel(ServicoApi api, ServicoSessao sessao)
        {
            _api = api;
            _sessao = sessao;

            AtualizarCommand = new Command(async () => await CarregarAsync());
            AlternarCandidatasCommand = new Command(() => MostrarCandidatas = !MostrarCandidatas);
            RemoverCompanheiraCommand = new Command(async () => await EscolherAsync(null));
            SairCommand = new Command(async () => await SairAsync());
        }

        public ICommand AtualizarCommand { get; }
        public ICommand AlternarCandidatasCommand { get; }
        public ICommand RemoverCompanheiraCommand { get; }
        public ICommand SairCommand { get; }

        public ObservableCollection<CandidataCompanheira> Candidatas { get; } = new ObservableCollection<CandidataCompanheira>();

        public string BoasVindas
        {
            get => _boasVindas;
            set => Definir(ref _boasVindas, value);
        }

        public bool EhAdmin
        {
            get => _ehAdmin;
            set => Definir(ref _ehAdmin, value);
        }

        public CandidataCompanheira? Companheira
        {
            get => _companheira;
            set
            {
                if (Definir(ref _companheira, value))
                {
                    Notificar(nameof(TemCompanheira));
                    Notificar(nameof(SemCompanheira));
                }
            }
        }

        public bool TemCompanheira => _companheira != null;

        /// <summary>Só é verdade depois de a API responder (para não piscar o aviso ao abrir).</summary>
        public bool SemCompanheira => _carregado && _companheira == null;

        public string Saudacao
        {
            get => _saudacao;
            set => Definir(ref _saudacao, value);
        }

        /// <summary>Avisos e erros mostrados por baixo do cartão.</summary>
        public string Mensagem
        {
            get => _mensagem;
            set
            {
                if (Definir(ref _mensagem, value))
                {
                    Notificar(nameof(TemMensagem));
                }
            }
        }

        public bool TemMensagem => !string.IsNullOrEmpty(_mensagem);

        public bool MostrarCandidatas
        {
            get => _mostrarCandidatas;
            set => Definir(ref _mostrarCandidatas, value);
        }

        /// <summary>"🔔 3 notificações por ler" (vazio quando não há).</summary>
        public string TextoNotificacoes
        {
            get => _textoNotificacoes;
            set
            {
                if (Definir(ref _textoNotificacoes, value))
                {
                    Notificar(nameof(TemNotificacoes));
                }
            }
        }

        public bool TemNotificacoes => !string.IsNullOrEmpty(_textoNotificacoes);

        public async Task CarregarAsync()
        {
            if (Ocupado)
            {
                return;
            }

            var utilizador = _sessao.Atual;
            if (utilizador == null)
            {
                await Shell.Current.GoToAsync("//login");
                return;
            }

            BoasVindas = "Olá, " + utilizador.NomeUtilizador + "!";
            EhAdmin = utilizador.IsAdmin;

            Ocupado = true;

            try
            {
                var resultado = await _api.ObterCompanheiraAsync(utilizador.Id);

                if (!resultado.Sucesso || resultado.Dados == null)
                {
                    Mensagem = resultado.Erro;
                    return;
                }

                Mensagem = string.Empty;
                _carregado = true;

                Saudacao = resultado.Dados.Saudacao ?? string.Empty;
                Companheira = resultado.Dados.Escolhida;
                Notificar(nameof(SemCompanheira));

                Candidatas.Clear();
                foreach (var candidata in resultado.Dados.Candidatas)
                {
                    Candidatas.Add(candidata);
                }

                // Sino: quantas notificações há por ler (se falhar, não se mostra nada)
                var contagem = await _api.ContarNotificacoesAsync(utilizador.Id);
                var naoLidas = contagem.Sucesso ? contagem.Dados : 0;
                TextoNotificacoes = naoLidas <= 0
                    ? string.Empty
                    : "🔔 " + naoLidas + (naoLidas == 1 ? " notificação por ler" : " notificações por ler");
            }
            finally
            {
                Ocupado = false;
            }
        }

        /// <summary>Escolhe (ou remove, com null) a companheira e volta a carregar a página.</summary>
        public async Task EscolherAsync(CandidataCompanheira? candidata)
        {
            var utilizador = _sessao.Atual;
            if (utilizador == null || Ocupado)
            {
                return;
            }

            Ocupado = true;
            ResultadoApi<string> resultado;

            try
            {
                resultado = await _api.EscolherCompanheiraAsync(utilizador.Id, candidata?.PersonagemId);
            }
            finally
            {
                Ocupado = false;
            }

            if (!resultado.Sucesso)
            {
                Mensagem = resultado.Erro;
                return;
            }

            MostrarCandidatas = false;
            await CarregarAsync();

            // A frase de confirmação da API fica visível depois de recarregar
            Mensagem = resultado.Dados ?? string.Empty;
        }

        private async Task SairAsync()
        {
            _sessao.Terminar();

            _carregado = false;
            Companheira = null;
            Saudacao = string.Empty;
            Mensagem = string.Empty;
            TextoNotificacoes = string.Empty;
            Candidatas.Clear();

            await Shell.Current.GoToAsync("//login");
        }
    }
}
