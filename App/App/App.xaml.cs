using App.Services;

namespace App
{
    public partial class App : Application
    {
        private readonly ILocationService _gps;
        private readonly GeofenceService _geofence;
        private readonly ITtsService _tts;

        public App(ILocationService gps, GeofenceService geofence, ITtsService tts)
        {
            InitializeComponent();
            _gps = gps;
            _geofence = geofence;
            _tts = tts;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

#if ANDROID
        protected override async void OnStart()
        {
            base.OnStart();

            // Xin quyền GPS
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return;

            // Khởi động Foreground Service
            var intent = new Android.Content.Intent(
                Android.App.Application.Context,
                typeof(LocationForegroundService)
            );
            Android.App.Application.Context.StartForegroundService(intent);

            // Bắt đầu GPS + kết nối luồng tự động thuyết minh
            await _gps.BatDauTheoDoiAsync(async (lat, lng) =>
            {
                var poi = await _geofence.KiemTraVungAsync(lat, lng);
                if (poi != null)
                    await _tts.PhatAmAsync(poi.MoTa_Vi, "vi-VN");
            });
        }
#endif
    }
}