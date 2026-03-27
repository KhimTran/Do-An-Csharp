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

            builder.Services.AddSingleton<LocalDatabase>();
            builder.Services.AddTransient<PoiListViewModel>();
            builder.Services.AddTransient<PoiListPage>();
            builder.Services.AddSingleton<GpsService>();
            builder.Services.AddTransient<MapViewModel>();
            builder.Services.AddTransient<MapPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}