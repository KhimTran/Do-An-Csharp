using App.Services;
using App.Views;
using Microsoft.Maui.Storage;

namespace App
{
    public partial class App : Application
    {
        public App(SyncService sync)
        {
            InitializeComponent();

            string appLanguage = Preferences.Get("app_language", Preferences.Get("tts_language", "vi-VN"));
            LocalizationResourceManager.Instance.SetLanguage(appLanguage);

            // Chay dong bo POI ngam khi app khoi dong.
            // Khong await de tranh chan UI.
            Task.Run(async () =>
            {
                await sync.EnsureSavedApiConfigurationLoadedAsync();
                await sync.DongBoPoisAsync();
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                return new Window(new AppShell());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Startup fallback: {ex}");
                return new Window(new StartupFallbackPage(ex.Message));
            }
        }
    }
}
