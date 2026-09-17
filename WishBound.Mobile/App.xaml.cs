namespace WishBound.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // O WishBound só tem tema escuro (como o site)
            UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell()) { Title = "WishBound" };
        }
    }
}
