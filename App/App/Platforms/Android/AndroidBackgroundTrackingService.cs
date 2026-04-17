#if ANDROID
using Android.Content;
using AndroidX.Core.Content;

namespace App.Services
{
    public class AndroidBackgroundTrackingService : IBackgroundTrackingService
    {
        private static bool _daKhoiDong;

        public Task StartAsync()
        {
            if (_daKhoiDong)
                return Task.CompletedTask;

            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(LocationForegroundService));
            ContextCompat.StartForegroundService(context, intent);
            _daKhoiDong = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(LocationForegroundService));
            context.StopService(intent);
            _daKhoiDong = false;
            return Task.CompletedTask;
        }
    }
}
#endif
