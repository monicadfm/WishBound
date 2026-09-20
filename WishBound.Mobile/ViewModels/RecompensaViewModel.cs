using System.Collections.ObjectModel;
using System.Windows.Input;
using WishBound.Mobile.Models;
using WishBound.Mobile.Services;

namespace WishBound.Mobile.ViewModels
{
    /// <summary>
    /// Aba Recompensa: saldos, o calendário de 28 dias da recompensa diária
    /// com o botão de receber, e os eventos a decorrer com a sua recompensa
    /// do dia. Mesmos endpoints que a página "Carteira" do site.
    /// </summary>
    public class RecompensaViewModel : BaseViewModel
    {
        private readonly ServicoApi _api;
        private readonly ServicoSessao _sessao;

        private string _saldoMoedas = "0";
        private string _saldoBilhetes = "0";
        private string _resumoCalendario = string.Empty;
        private string _textoBotao = "Reclamar recompensa";
        private bool _podeReceber;
        private double _fracaoCiclo;
        private string _mensagem = string.Empty;
        private bool _temEventos;
        private bool _carregado;

        public RecompensaViewModel(ServicoApi api, ServicoSessao sessao)
        {
            _api = api;
            _sessao = sessao;

            AtualizarCommand = new Command(async () => await CarregarAsync());
            ReceberCommand = new Command(async () => await ReceberDiariaAsync());
        }

        public ICommand AtualizarCommand { get; }
        public ICommand ReceberCommand { get; }

        public ObservableCollection<DiaRecompensa> Calendario { get; } = new ObservableCollection<DiaRecompensa>();
        public ObservableCollection<EventoResposta> Eventos { get; } = new ObservableCollection<EventoResposta>();

        public string SaldoMoedas
        {
            get => _saldoMoedas;
            set => Definir(ref _saldoMoedas, value);
        }

        public string SaldoBilhetes
        {
            get => _saldoBilhetes;
            set => Definir(ref _saldoBilhetes, value);
        }

        /// <summary>"Semana 2 de 4 · 9 / 28 dias"</summary>
        public string ResumoCalendario
        {
            get => _resumoCalendario;
            set => Definir(ref _resumoCalendario, value);
        }

        public string TextoBotao
        {
            get => _textoBotao;
            set => Definir(ref _textoBotao, value);
        }

        public bool PodeReceber
        {
            get => _podeReceber;
            set => Definir(ref _podeReceber, value);
        }

        /// <summary>DiasRecebidos / 28, para a barra.</summary>
        public double FracaoCiclo
        {
            get => _fracaoCiclo;
            set => Definir(ref _fracaoCiclo, value);
        }

        public bool TemEventos
        {
            get => _temEventos;
            set
            {
                if (Definir(ref _temEventos, value))
                {
                    Notificar(nameof(SemEventos));
                }
            }
        }

        public bool SemEventos => _carregado && !_temEventos;

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
                var resultado = await _api.ObterEconomiaAsync(utilizador.Id);

                if (!resultado.Sucesso || resultado.Dados == null)
                {
                    Mensagem = resultado.Erro;
                    return;
                }

                Mensagem = string.Empty;
                _carregado = true;
                var dados = resultado.Dados;

                SaldoMoedas = dados.SaldoMoedas.ToString("0");
                SaldoBilhetes = dados.SaldoBilhetes.ToString("0");

                var diaria = dados.RecompensaDiaria;

                Calendario.Clear();
                foreach (var dia in diaria.Calendario)
                {
                    Calendario.Add(dia);
                }

                var total = Calendario.Count > 0 ? Calendario.Count : 28;
                var semana = Calendario.FirstOrDefault(d => d.Hoje)?.Semana ?? ((diaria.ProximoDia - 1) / 7 + 1);
                ResumoCalendario = "Semana " + semana + " de 4 · " + diaria.DiasRecebidos + " / " + total + " dias";
                FracaoCiclo = Math.Clamp(diaria.DiasRecebidos / (double)total, 0, 1);

                PodeReceber = !diaria.RecebidaHoje;

                if (diaria.RecebidaHoje)
                {
                    TextoBotao = "✓ Recebida hoje · volta amanhã";
                }
                else
                {
                    var hoje = Calendario.FirstOrDefault(d => d.Hoje) ?? Calendario.FirstOrDefault(d => d.Dia == diaria.ProximoDia);
                    TextoBotao = hoje == null
                        ? "Reclamar recompensa"
                        : "Reclamar dia " + hoje.Dia + " (" + hoje.Valor + " " + hoje.MoedaNome + ")";
                }

                Eventos.Clear();
                foreach (var evento in dados.Eventos)
                {
                    Eventos.Add(evento);
                }
                TemEventos = Eventos.Count > 0;
                Notificar(nameof(SemEventos));
            }
            finally
            {
                Ocupado = false;
            }
        }

        private async Task ReceberDiariaAsync()
        {
            var utilizador = _sessao.Atual;
            if (utilizador == null || Ocupado)
            {
                return;
            }

            Ocupado = true;
            ResultadoApi<RecompensaRecebidaResposta> resultado;

            try
            {
                resultado = await _api.ReceberLoginDiarioAsync(utilizador.Id);
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

            await CarregarAsync();
            Mensagem = TextoRecebida(resultado.Dados);
        }

        /// <summary>Resgata a recompensa de hoje de um evento (chamado pela página).</summary>
        public async Task ResgatarEventoAsync(EventoResposta evento)
        {
            var utilizador = _sessao.Atual;
            if (utilizador == null || Ocupado)
            {
                return;
            }

            Ocupado = true;
            ResultadoApi<RecompensaRecebidaResposta> resultado;

            try
            {
                resultado = await _api.ResgatarEventoAsync(utilizador.Id, evento.BannerId);
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

            await CarregarAsync();
            Mensagem = TextoRecebida(resultado.Dados);
        }

        private static string TextoRecebida(RecompensaRecebidaResposta r)
        {
            var texto = string.IsNullOrEmpty(r.Mensagem)
                ? "Recebeste " + r.Quantidade.ToString("0") + " " + r.MoedaNome + "!"
                : r.Mensagem;

            return texto + " Saldo: " + r.NovoSaldo.ToString("0") + " " + r.MoedaNome + ".";
        }
    }
}
