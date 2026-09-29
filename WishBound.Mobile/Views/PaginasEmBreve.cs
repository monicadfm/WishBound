namespace WishBound.Mobile.Views
{
    // ============================================================
    //  Base das abas "Em breve" usada enquanto a app estava a ser
    //  construída. Todas as abas já têm página própria, por isso
    //  este ficheiro já não é usado e pode ser apagado.
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
                    new Label { Text = titulo, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#dbafa0"), HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = "Em breve", FontSize = 16, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = descricao, TextColor = Color.FromArgb("#c9a0ab"), HorizontalTextAlignment = TextAlignment.Center }
                }
            };
        }
    }
}
