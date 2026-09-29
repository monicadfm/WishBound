using WishBound.Mobile.Services;

namespace WishBound.Mobile
{
    public partial class App : Application
    {
        /// <summary>De quanto em quanto tempo, com a app aberta, se procuram notificações novas.</summary>
        private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromSeconds(60);

        private readonly ServicoLembretesTelemovel _lembretes;
        private bool _emPrimeiroPlano;
        private bool _relogioLigado;

        public App(ServicoLembretesTelemovel lembretes)
        {
            InitializeComponent();
            _lembretes = lembretes;

            // O WishBound só tem tema escuro (como o site)
            UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell()) { Title = "WishBound" };
        }

        // A app abriu ou voltou ao primeiro plano: mostra as notificações
        // novas do servidor e volta a verificar de minuto a minuto.
        protected override void OnStart()
        {
            base.OnStart();
            ComecarVerificacao();
        }

        protected override void OnResume()
        {
            base.OnResume();
            ComecarVerificacao();
        }

        // Em segundo plano deixa de verificar (os alarmes agendados continuam)
        protected override void OnSleep()
        {
            base.OnSleep();
            _emPrimeiroPlano = false;
        }

        private void ComecarVerificacao()
        {
            _emPrimeiroPlano = true;
            _ = _lembretes.MostrarNovasNotificacoesAsync();

            if (_relogioLigado)
            {
                return;
            }

            _relogioLigado = true;
            Dispatcher.StartTimer(IntervaloVerificacao, () =>
            {
                if (!_emPrimeiroPlano)
                {
                    _relogioLigado = false;
                    return false;
                }

                _ = _lembretes.MostrarNovasNotificacoesAsync();
                return true;
            });
        }
    }
}
