using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using App.Services;
using App.ViewModels;
using App.Views;

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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // 1. ĐĂNG KÝ DATABASE & CÁC SERVICES (MÌNH ĐÃ BỔ SUNG ĐẦY ĐỦ Ở ĐÂY)
            builder.Services.AddSingleton<LocalDatabase>();
            builder.Services.AddSingleton<GpsService>();
            builder.Services.AddSingleton<GeofenceService>(); // <-- Sửa lỗi sập app
            builder.Services.AddSingleton<ITtsService, TtsService>(); // <-- Sửa lỗi sập app
            builder.Services.AddSingleton<ILocationService, LocationService>();

            // 2. ĐĂNG KÝ VIEW MODELS
            builder.Services.AddTransient<PoiListViewModel>();
            builder.Services.AddTransient<MapViewModel>();

            // 3. ĐĂNG KÝ VIEWS (PAGES)
            builder.Services.AddTransient<PoiListPage>();
            builder.Services.AddTransient<MapPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}