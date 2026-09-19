using System.Collections.ObjectModel;
using System.Windows.Input;
using WishBound.Mobile.Models;
using WishBound.Mobile.Services;

namespace WishBound.Mobile.ViewModels
{
    /// <summary>
    /// Detalhes de uma personagem da coleção: imagem, descrição, amizade
    /// (nível, barra, Interagir), saudação, conjunto de mensagens com os
    /// bloqueios, favorita e "tornar companheira".
    /// </summary>
    public class DetalhesPersonagemViewModel : BaseViewModel
    {
        private readonly ServicoApi _api;
        private readonly ServicoSessao _sessao;

        private int _personagemId;
        private ItemColecao? _item;
        private string _saudacao = string.Empty;
        private bool _ehCompanheira;
        private string _resumoMensagens = string.Empty;
        private string _reacao = string.Empty;
        private string _mensagem = string.Empty;
        private bool _mostrarMensagens;
        private WebViewSource? _imagem;

        public DetalhesPersonagemViewModel(ServicoApi api, ServicoSessao sessao)
        {
            _api = api;
            _sessao = sessao;

            FavoritoCommand = new Command(async () => await AlternarFavoritoAsync());
            CompanheiraCommand = new Command(async () => await TornarCompanheiraAsync());
            InteragirCommand = new Command(async () => await InteragirAsync());
            AlternarMensagensCommand = new Command(() => MostrarMensagens = !MostrarMensagens);
        }

        public ICommand FavoritoCommand { get; }
        public ICommand CompanheiraCommand { get; }
        public ICommand InteragirCommand { get; }
        public ICommand AlternarMensagensCommand { get; }

        public ObservableCollection<MensagemPersonagem> Mensagens { get; } = new ObservableCollection<MensagemPersonagem>();

        public int PersonagemId
        {
            get => _personagemId;
            set => Definir(ref _personagemId, value);
        }

        public ItemColecao? Item
        {
            get => _item;
            set
            {
                if (Definir(ref _item, value))
                {
                    Notificar(nameof(TemItem));
                    Notificar(nameof(TextoFavorito));
                }
            }
        }

        public bool TemItem => _item != null;

        public string TextoFavorito => _item != null && _item.IsFavorito ? "★ Favorita" : "☆ Marcar favorita";

        /// <summary>Imagem da personagem, vinda do site, mostrada num WebView (a arte é SVG).</summary>
        public WebViewSource? Imagem
        {
            get => _imagem;
            set
            {
                if (Definir(ref _imagem, value))
                {
                    Notificar(nameof(TemImagem));
                }
            }
        }

        public bool TemImagem => _imagem != null;

        public string Saudacao
        {
            get => _saudacao;
            set
            {
                if (Definir(ref _saudacao, value))
                {
                    Notificar(nameof(TemSaudacao));
                }
            }
        }

        public bool TemSaudacao => !string.IsNullOrEmpty(_saudacao);

        public bool EhCompanheira
        {
            get => _ehCompanheira;
            set
            {
                if (Definir(ref _ehCompanheira, value))
                {
                    Notificar(nameof(TextoCompanheira));
                }
            }
        }

        public string TextoCompanheira => _ehCompanheira ? "♡ É a tua companheira" : "Tornar companheira";

        public string ResumoMensagens
        {
            get => _resumoMensagens;
            set => Definir(ref _resumoMensagens, value);
        }

        public bool MostrarMensagens
        {
            get => _mostrarMensagens;
            set => Definir(ref _mostrarMensagens, value);
        }

        /// <summary>O que a personagem "disse" na última interação.</summary>
        public string Reacao
        {
            get => _reacao;
            set
            {
                if (Definir(ref _reacao, value))
                {
                    Notificar(nameof(TemReacao));
                }
            }
        }

        public bool TemReacao => !string.IsNullOrEmpty(_reacao);

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

        public async Task CarregarAsync()
        {
            if (Ocupado || _personagemId <= 0)
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
                var resultado = await _api.ObterItemColecaoAsync(utilizador.Id, _personagemId);

                if (!resultado.Sucesso || resultado.Dados == null)
                {
                    Mensagem = resultado.Erro;
                    return;
                }

                Mensagem = string.Empty;
                Item = resultado.Dados;
                Imagem = CriarImagem(resultado.Dados.ImagemCompleta);

                // Saudação + conjunto de mensagens (se falhar, a página abre na mesma)
                var mensagens = await _api.ObterMensagensPersonagemAsync(utilizador.Id, _personagemId);

                Mensagens.Clear();

                if (mensagens.Sucesso && mensagens.Dados != null)
                {
                    Saudacao = mensagens.Dados.Saudacao ?? string.Empty;
                    EhCompanheira = mensagens.Dados.EhCompanheira;
                    ResumoMensagens = "Mensagens de " + mensagens.Dados.Nome + " · " +
                        mensagens.Dados.Desbloqueadas + " de " + mensagens.Dados.Total + " desbloqueadas";

                    foreach (var m in mensagens.Dados.Mensagens)
                    {
                        Mensagens.Add(m);
                    }
                }
                else
                {
                    Saudacao = string.Empty;
                    ResumoMensagens = string.Empty;
                }
            }
            finally
            {
                Ocupado = false;
            }
        }

        private async Task AlternarFavoritoAsync()
        {
            var utilizador = _sessao.Atual;
            if (utilizador == null || _item == null || Ocupado)
            {
                return;
            }

            Ocupado = true;
            ResultadoApi<string> resultado;

            try
            {
                resultado = await _api.MarcarFavoritoAsync(utilizador.Id, _item.PersonagemId, !_item.IsFavorito);
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
            Mensagem = resultado.Dados ?? string.Empty;
        }

        private async Task TornarCompanheiraAsync()
        {
            var utilizador = _sessao.Atual;
            if (utilizador == null || _item == null || Ocupado || _ehCompanheira)
            {
                return;
            }

            Ocupado = true;
            ResultadoApi<string> resultado;

            try
            {
                resultado = await _api.EscolherCompanheiraAsync(utilizador.Id, _item.PersonagemId);
            }
            finally
            {
                Ocupado = false;
            }

            Mensagem = resultado.Sucesso ? (resultado.Dados ?? string.Empty) : resultado.Erro;

            if (resultado.Sucesso)
            {
                EhCompanheira = true;
            }
        }

        private async Task InteragirAsync()
        {
            var utilizador = _sessao.Atual;
            if (utilizador == null || _item == null || Ocupado)
            {
                return;
            }

            Ocupado = true;
            ResultadoApi<InteracaoResposta> resultado;

            try
            {
                resultado = await _api.InteragirAsync(utilizador.Id, _item.PersonagemId);
            }
            finally
            {
                Ocupado = false;
            }

            if (!resultado.Sucesso || resultado.Dados == null)
            {
                Mensagem = resultado.Erro;
                return;
            }

            // Recarrega primeiro (a barra e o nível ficam certos), depois mostra o resultado
            await CarregarAsync();

            var r = resultado.Dados;
            Reacao = r.Reacao;

            var texto = "+" + r.PontosGanhos + " pontos de amizade";
            if (r.SubiuDeNivel)
            {
                texto += " · subiu para " + r.NivelAtual + "!";
            }
            if (r.NivelMaximo)
            {
                texto += " · nível máximo";
            }
            if (!utilizador.IsAdmin)
            {
                texto += " · faltam " + r.InteracoesRestantes + " interações hoje";
            }
            Mensagem = texto;
        }

        /// <summary>
        /// A arte das personagens é SVG e o controlo Image do MAUI não carrega
        /// SVG por URL, por isso a imagem é mostrada numa página HTML mínima
        /// dentro de um WebView.
        /// </summary>
        private static WebViewSource? CriarImagem(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            var html =
                "<html><head><meta name='viewport' content='width=device-width, initial-scale=1'></head>" +
                "<body style='margin:0;background:#1e1830;display:flex;align-items:center;justify-content:center;height:100vh;overflow:hidden'>" +
                "<img src='" + url + "' style='max-width:100%;max-height:100%' alt=''></body></html>";

            return new HtmlWebViewSource { Html = html };
        }
    }
}
