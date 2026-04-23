using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.OS;

namespace App
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationForegroundService : Android.App.Service
    {
        private const string ChannelId = "location_channel";

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(
            Intent? intent, StartCommandFlags flags, int startId)
        {
            var manager = (NotificationManager)GetSystemService(NotificationService)!;
            var notification = OperatingSystem.IsAndroidVersionAtLeast(26)
                ? BuildNotificationForAndroid26Plus(manager)
                : BuildLegacyNotification();

            StartForeground(1, notification);
            return StartCommandResult.Sticky;
        }

        [SupportedOSPlatform("android26.0")]
        private Notification BuildNotificationForAndroid26Plus(NotificationManager manager)
        {
            var channel = new NotificationChannel(
                ChannelId,
                "Theo doi vi tri",
                NotificationImportance.Low);

            manager.CreateNotificationChannel(channel);

            return new Notification.Builder(this, ChannelId)
                .SetContentTitle("Vinh Khanh")
                .SetContentText("Dang theo doi vi tri de phat thuyet minh...")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .Build();
        }

        [UnsupportedOSPlatform("android26.0")]
        private Notification BuildLegacyNotification()
        {
            return new Notification.Builder(this)
                .SetContentTitle("Vinh Khanh")
                .SetContentText("Dang theo doi vi tri de phat thuyet minh...")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .Build();
        }
    }
}
