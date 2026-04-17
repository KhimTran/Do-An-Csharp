using App.Services;
using App.ViewModels;
using App.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;


namespace App
{
    public static class MauiProgram
    {
        public static IServiceProvider? Services { get; private set; }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<LocalDatabase>();
            builder.Services.AddSingleton<SyncService>();
            builder.Services.AddSingleton<GeofenceService>();
            builder.Services.AddSingleton<ITtsService, TtsService>();
            builder.Services.AddSingleton<ILocationService, LocationService>();
#if ANDROID
            builder.Services.AddSingleton<IBackgroundTrackingService, AndroidBackgroundTrackingService>();
#else
            builder.Services.AddSingleton<IBackgroundTrackingService, NoopBackgroundTrackingService>();
#endif

            builder.Services.AddTransient<PoiListViewModel>();
            builder.Services.AddTransient<MapViewModel>();
            builder.Services.AddTransient<QrScanViewModel>();
            builder.Services.AddTransient<HistoryViewModel>();

            builder.Services.AddTransient<PoiListPage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<QrScanPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddSingleton<AnalyticsService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            Services = app.Services;
            return app;
        }
    }
}
