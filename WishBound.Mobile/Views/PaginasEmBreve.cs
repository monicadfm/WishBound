namespace WishBound.Mobile.Views
{
    // ============================================================
    //  Abas que ainda não foram construídas. Ficam já na barra para
    //  a estrutura da app estar completa; cada uma vai ser trocada
    //  por uma página a sério (XAML + view model) nas próximas fases.
    // ============================================================
    public abstract class PaginaEmBreve : ContentPage
    {
        protected PaginaEmBreve(string titulo, string descricao)
        {
            Title = titulo;

            Content = new VerticalStackLayout
            {
                Padding = new Thickness(28),
                Spacing = 10,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = titulo,
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#f3c04f"),
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = "Em breve",
                        FontSize = 16,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = descricao,
                        TextColor = Color.FromArgb("#a79fc4"),
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            };
        }
    }

    public class ColecaoPage : PaginaEmBreve
    {
        public ColecaoPage()
            : base("Coleção", "As tuas personagens, com raridade, cópias, favoritas e nível de amizade.")
        {
        }
    }

    public class RecompensaPage : PaginaEmBreve
    {
        public RecompensaPage()
            : base("Recompensa diária", "O calendário de 28 dias e o botão para receberes a recompensa de hoje.")
        {
        }
    }

    public class NotificacoesPage : PaginaEmBreve
    {
        public NotificacoesPage()
            : base("Notificações", "As mensagens das tuas personagens e os avisos de eventos.")
        {
        }
    }
}
