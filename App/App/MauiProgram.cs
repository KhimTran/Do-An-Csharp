using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using App.Services;
using App.ViewModels;   // ← THÊM
using App.Views;        // ← THÊM
namespace App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()  // <-- Thêm dòng này
                .UseMauiMaps()          // <-- Thêm dòng này (nếu dùng Maps)
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<LocalDatabase>();
            builder.Services.AddTransient<PoiListViewModel>();  // ← THÊM
            builder.Services.AddTransient<PoiListPage>();       // ← THÊM
            builder.Services.AddSingleton<GpsService>();   // ← THÊM DÒNG NÀY
            builder.Services.AddTransient<MapViewModel>();   // ← THÊM
            builder.Services.AddTransient<MapPage>();        // ← THÊM
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
