using App.Services;
using App.ViewModels;
using App.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using ZXing.Net.Maui.Controls;


namespace App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseSkiaSharp()
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

            return builder.Build();
        }
    }
}
