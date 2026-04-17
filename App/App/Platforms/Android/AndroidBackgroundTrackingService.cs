#if ANDROID
using Android.Content;
using AndroidX.Core.Content;

namespace App.Services
{
    public class AndroidBackgroundTrackingService : IBackgroundTrackingService
    {
        public Task StartAsync()
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(LocationForegroundService));
            ContextCompat.StartForegroundService(context, intent);
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(LocationForegroundService));
            context.StopService(intent);
            return Task.CompletedTask;
        }
    }
}
#endif
