using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace App
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationForegroundService : Android.App.Service
    {
        public const string CHANNEL_ID = "gps_channel";
        public const int NOTIF_ID = 1001;

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            TaoBieuTuong();

            var notification = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle("Vĩnh Khánh Tour")
                .SetContentText("Đang theo dõi vị trí để phát thuyết minh...")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true)
                .Build();

            StartForeground(NOTIF_ID, notification);
            return StartCommandResult.Sticky;
        }

        private void TaoBieuTuong()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    CHANNEL_ID,
                    "GPS Theo dõi vị trí",
                    NotificationImportance.Low
                );
                var manager = (NotificationManager?)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }
    }
}