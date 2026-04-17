using App.Services;
using Microsoft.Extensions.DependencyInjection;
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

            // Chạy đồng bộ POI ngầm khi app khởi động
            // Không await để không chặn UI
            Task.Run(async () => await sync.DongBoPoisAsync());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}