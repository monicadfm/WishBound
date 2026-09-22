using System.Collections.ObjectModel;
using System.Windows.Input;
using WishBound.Mobile.Models;
using WishBound.Mobile.Services;

namespace WishBound.Mobile.ViewModels
{
    /// <summary>
    /// Aba Notificações: as mensagens diárias das personagens e as subidas
    /// de nível de amizade (tabela Notificacoes), mais recentes primeiro.
    /// Tocar numa marca-a como lida; há também "marcar todas".
    /// </summary>
    public class NotificacoesViewModel : BaseViewModel
    {
        private readonly ServicoApi _api;
        private readonly ServicoSessao _sessao;

        private bool _soNaoLidas;
        private string _resumo = string.Empty;
        private bool _temNaoLidas;
        private string _mensagem = string.Empty;
        private bool _vazia;

        public NotificacoesViewModel(ServicoApi api, ServicoSessao sessao)
        {
            _api = api;
            _sessao = sessao;

            AtualizarCommand = new Command(async () => await CarregarAsync());
            AlternarFiltroCommand = new Command(async () =>
            {
                SoNaoLidas = !SoNaoLidas;
                await CarregarAsync();
            });
            MarcarTodasCommand = new Command(async () => await MarcarLidaAsync(null));
        }

        public ICommand AtualizarCommand { get; }
        public ICommand AlternarFiltroCommand { get; }
        public ICommand MarcarTodasCommand { get; }

        public ObservableCollection<Notificacao> Itens { get; } = new ObservableCollection<Notificacao>();

        public bool SoNaoLidas
        {
            get => _soNaoLidas;
            set
            {
                if (Definir(ref _soNaoLidas, value))
                {
                    Notificar(nameof(TextoFiltro));
                }
            }
        }

        public string TextoFiltro => _soNaoLidas ? "Só por ler" : "Todas";

        /// <summary>"3 por ler · 12 no total"</summary>
        public string Resumo
        {
            get => _resumo;
            set => Definir(ref _resumo, value);
        }

        public bool TemNaoLidas
        {
            get => _temNaoLidas;
            set => Definir(ref _temNaoLidas, value);
        }

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

        public bool Vazia
        {
            get => _vazia;
            set => Definir(ref _vazia, value);
        }

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

            Ocupado = true;

            try
            {
                var resultado = await _api.ObterNotificacoesAsync(utilizador.Id, _soNaoLidas);

                if (!resultado.Sucesso || resultado.Dados == null)
                {
                    Mensagem = resultado.Erro;
                    return;
                }

                Mensagem = string.Empty;
                var dados = resultado.Dados;

                Itens.Clear();
                foreach (var item in dados.Itens)
                {
                    Itens.Add(item);
                }

                Vazia = Itens.Count == 0;
                TemNaoLidas = dados.NaoLidas > 0;
                Resumo = dados.NaoLidas + " por ler · " + dados.Total + " no total";
            }
            finally
            {
                Ocupado = false;
            }
        }

        /// <summary>Marca uma notificação (ou todas, com null) como lida e recarrega.</summary>
        public async Task MarcarLidaAsync(Notificacao? notificacao)
        {
            var utilizador = _sessao.Atual;
            if (utilizador == null || Ocupado)
            {
                return;
            }

            if (notificacao != null && notificacao.IsLida)
            {
                return;
            }

            Ocupado = true;
            ResultadoApi<string> resultado;

            try
            {
                resultado = await _api.MarcarNotificacaoLidaAsync(utilizador.Id, notificacao?.Id);
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

            await CarregarAsync();

            if (notificacao == null)
            {
                Mensagem = resultado.Dados ?? string.Empty;
            }
        }
    }
}
