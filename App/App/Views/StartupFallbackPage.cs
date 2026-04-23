namespace App.Views;

public sealed class StartupFallbackPage : ContentPage
{
    public StartupFallbackPage(string detail)
    {
        Title = "App startup";

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "App could not render the normal shell.",
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 20
                    },
                    new Label
                    {
                        Text = "A fallback page is shown so the app does not stay on a black screen. Please check the startup error below.",
                        FontSize = 14
                    },
                    new Label
                    {
                        Text = detail,
                        FontSize = 13
                    }
                }
            }
        };
    }
}
