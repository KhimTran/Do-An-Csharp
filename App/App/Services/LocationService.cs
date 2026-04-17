namespace App.Services
{
    public class LocationService : ILocationService
    {
        private readonly IBackgroundTrackingService _backgroundTracking;
        private CancellationTokenSource? _cts;

        public LocationService(IBackgroundTrackingService backgroundTracking)
        {
            _backgroundTracking = backgroundTracking;
        }

        public async Task BatDauTheoDoiAsync(Action<double, double> khiCoViTri)
        {
            var trangThai = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (trangThai != PermissionStatus.Granted)
                throw new Exception("Không có quyền truy cập GPS.");

            // Chỉ bật foreground service sau khi có quyền, tránh crash lúc app vừa mở.
            _ = Task.Run(async () =>
            {
                try
                {
                    await _backgroundTracking.StartAsync();
                }
                catch
                {
                    // Không làm hỏng luồng GPS foreground nếu service nền không khởi động được.
                }
            });

            // Nếu đang chạy rồi thì không chạy thêm lần nữa
            if (_cts != null && !_cts.IsCancellationRequested)
                return;

            _cts = new CancellationTokenSource();

            // Chụp token ra biến local để tránh bị null khi DungTheoDoi() chạy
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var viTri = await Geolocation.GetLocationAsync(
                            new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)),
                            token);

                        if (viTri != null)
                        {
                            khiCoViTri(viTri.Latitude, viTri.Longitude);
                            System.Diagnostics.Debug.WriteLine($"[GPS] {viTri.Latitude}, {viTri.Longitude}");
                        }
                    }
                    catch (FeatureNotEnabledException)
                    {
                        System.Diagnostics.Debug.WriteLine("[GPS] GPS đang tắt.");
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GPS] Lỗi: {ex.Message}");
                    }

                    try
                    {
                        await Task.Delay(3000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        public void DungTheoDoi()
        {
            if (_cts == null)
                return;

            try
            {
                _cts.Cancel();
            }
            catch
            {
            }

            _cts.Dispose();
            _cts = null;
        }

        public async Task<(double Lat, double Lng)?> LayViTriHienTaiAsync()
        {
            var viTri = await Geolocation.GetLastKnownLocationAsync();
            if (viTri == null) return null;

            return (viTri.Latitude, viTri.Longitude);
        }
    }
}
