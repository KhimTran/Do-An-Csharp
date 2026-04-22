using App.Services;
using App.ViewModels;
using App.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;

#if ANDROID
using Android.Webkit;
using Microsoft.Maui.Handlers;
#endif

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
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if ANDROID
            WebViewHandler.Mapper.AppendToMapping("LeafletWebView", (handler, view) =>
            {
                handler.PlatformView.Settings.JavaScriptEnabled = true;
                handler.PlatformView.Settings.DomStorageEnabled = true;
                handler.PlatformView.Settings.AllowFileAccess = true;
                handler.PlatformView.Settings.AllowContentAccess = true;
                handler.PlatformView.Settings.MixedContentMode = MixedContentHandling.CompatibilityMode;
                handler.PlatformView.Settings.SetSupportZoom(true);
                handler.PlatformView.Settings.BuiltInZoomControls = false;
                handler.PlatformView.Settings.DisplayZoomControls = false;
            });
#endif

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
