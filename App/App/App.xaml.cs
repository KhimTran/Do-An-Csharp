using App.Services;
using Microsoft.Maui.Storage;

namespace App
{
    public partial class App : Application
    {
        public App(SyncService sync, IBackgroundTrackingService backgroundTracking)
        {
            InitializeComponent();

            string appLanguage = Preferences.Get("app_language", Preferences.Get("tts_language", "vi-VN"));
            LocalizationResourceManager.Instance.SetLanguage(appLanguage);

            // Chạy đồng bộ POI ngầm khi app khởi động
            // Không await để không chặn UI
            Task.Run(async () => await sync.DongBoPoisAsync());

            // Khởi động theo dõi nền (Android foreground service).
            Task.Run(async () => await backgroundTracking.StartAsync());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
