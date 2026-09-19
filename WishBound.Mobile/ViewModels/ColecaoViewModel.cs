using System.Collections.ObjectModel;
using System.Windows.Input;
using WishBound.Mobile.Models;
using WishBound.Mobile.Services;

namespace WishBound.Mobile.ViewModels
{
    /// <summary>
    /// Aba Coleção: as personagens do utilizador em grelha, com ordenação,
    /// filtro de favoritas e o resumo do espaço. No telemóvel a coleção é
    /// só de leitura, exceto marcar favoritas (e a companheira, nos detalhes).
    /// </summary>
    public class ColecaoViewModel : BaseViewModel
    {
        private readonly ServicoApi _api;
        private readonly ServicoSessao _sessao;

        // Mesmos valores que a API aceita em ?ordenar=
        private static readonly string[] ValoresOrdenacao = { "raridade", "nome", "data" };

        private int _ordenacaoIndice;
        private bool _soFavoritas;
        private string _resumoEspaco = string.Empty;
        private string _resumoPersonagens = string.Empty;
        private string _resumoInteracoes = string.Empty;
        private double _fracaoEspaco;
        private string _mensagem = string.Empty;
        private bool _vazia;

        public ColecaoViewModel(ServicoApi api, ServicoSessao sessao)
        {
            _api = api;
            _sessao = sessao;

            AtualizarCommand = new Command(async () => await CarregarAsync());
            AlternarFavoritasCommand = new Command(async () =>
            {
                SoFavoritas = !SoFavoritas;
                await CarregarAsync();
            });
        }

        public ICommand AtualizarCommand { get; }
        public ICommand AlternarFavoritasCommand { get; }

        public ObservableCollection<ItemColecao> Itens { get; } = new ObservableCollection<ItemColecao>();

        /// <summary>Opções do Picker de ordenação (mesma ordem de ValoresOrdenacao).</summary>
        public List<string> Ordenacoes { get; } = new List<string> { "Raridade", "Nome", "Data" };

        public int OrdenacaoIndice
        {
            get => _ordenacaoIndice;
            set
            {
                if (Definir(ref _ordenacaoIndice, value))
                {
                    _ = CarregarAsync();
                }
            }
        }

        public bool SoFavoritas
        {
            get => _soFavoritas;
            set
            {
                if (Definir(ref _soFavoritas, value))
                {
                    Notificar(nameof(TextoFavoritas));
                }
            }
        }

        public string TextoFavoritas => _soFavoritas ? "★ Só favoritas" : "☆ Todas";

        public string ResumoEspaco
        {
            get => _resumoEspaco;
            set => Definir(ref _resumoEspaco, value);
        }

        public string ResumoPersonagens
        {
            get => _resumoPersonagens;
            set => Definir(ref _resumoPersonagens, value);
        }

        public string ResumoInteracoes
        {
            get => _resumoInteracoes;
            set => Definir(ref _resumoInteracoes, value);
        }

        /// <summary>Ocupado / Capacidade, para a barra (0..1).</summary>
        public double FracaoEspaco
        {
            get => _fracaoEspaco;
            set => Definir(ref _fracaoEspaco, value);
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

        /// <summary>true depois de carregar sem itens (mostra o aviso em vez da grelha vazia).</summary>
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
                var ordenar = ValoresOrdenacao[Math.Clamp(_ordenacaoIndice, 0, ValoresOrdenacao.Length - 1)];
                var resultado = await _api.ObterColecaoAsync(utilizador.Id, ordenar, _soFavoritas);

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
                ResumoEspaco = dados.Ocupado + " / " + dados.Capacidade + " lugares";
                ResumoPersonagens = dados.PersonagensDistintas + " de " + dados.PersonagensExistentes + " personagens";
                FracaoEspaco = dados.Capacidade > 0 ? Math.Clamp(dados.Ocupado / (double)dados.Capacidade, 0, 1) : 0;

                if (utilizador.IsAdmin)
                {
                    ResumoInteracoes = "Interações de hoje: ilimitadas (admin)";
                }
                else
                {
                    ResumoInteracoes = "Interações de hoje: " + dados.InteracoesRestantes + " de " + dados.InteracoesPorDia;
                }
            }
            finally
            {
                Ocupado = false;
            }
        }

        /// <summary>Marca/desmarca a estrela e volta a carregar a lista.</summary>
        public async Task AlternarFavoritoAsync(ItemColecao item)
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
                resultado = await _api.MarcarFavoritoAsync(utilizador.Id, item.PersonagemId, !item.IsFavorito);
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
        }
    }
}
