// Platforms/Android/LocationForegroundService.cs
using Android.App;
using Android.Content;
using Android.OS;

namespace App
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationForegroundService : Android.App.Service
    {
        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(
            Intent? intent, StartCommandFlags flags, int startId)
        {
            // Tạo notification để Android cho phép chạy nền
            var channel = new NotificationChannel(
                "location_channel", "Theo dõi vị trí",
                NotificationImportance.Low
            );

            var manager = (NotificationManager)GetSystemService(NotificationService)!;
            manager.CreateNotificationChannel(channel);

            var notification = new Notification.Builder(this, "location_channel")
                .SetContentTitle("Vĩnh Khánh")
                .SetContentText("Đang theo dõi vị trí để phát thuyết minh...")
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .Build();

            StartForeground(1, notification);
            return StartCommandResult.Sticky;
        }
    }
}