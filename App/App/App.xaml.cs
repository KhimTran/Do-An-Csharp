using Microsoft.Extensions.DependencyInjection;
using App.Services; // Thêm dòng này

namespace App
{
    public partial class App : Application
    {
        private readonly GpsService _gps;
        private readonly GeofenceService _geofence;
        private readonly ITtsService _tts;

        // Bổ sung các service vào tham số hàm App()
        public App(GpsService gps, GeofenceService geofence, ITtsService tts)
        {
            InitializeComponent();
            _gps = gps;
            _geofence = geofence;
            _tts = tts;

            // KẾT NỐI LUỒNG TỰ ĐỘNG THUYẾT MINH Ở ĐÂY
            _gps.OnViTriMoi += async (viTri) =>
            {
                // Khi có GPS mới (mỗi 3 giây), kiểm tra xem có ở gần POI nào không
                var poi = await _geofence.KiemTraVungAsync(viTri.Latitude, viTri.Longitude);

                // Nếu có lọt vào vùng bán kính của 1 POI -> gọi TTS phát âm
                if (poi != null)
                {
                    await _tts.PhatAmAsync(poi.MoTa_Vi, "vi-VN");
                    // Tạm thời fix cứng tiếng Việt, sau này sẽ lấy theo cài đặt ngôn ngữ
                }
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

#if ANDROID
        protected override async void OnStart()
        {
            base.OnStart();
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return;

            var intent = new Android.Content.Intent(
                Android.App.Application.Context,
                typeof(LocationForegroundService)
            );
            Android.App.Application.Context.StartForegroundService(intent);
        }
#endif
    }
}