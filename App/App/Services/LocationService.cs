// Services/LocationService.cs
namespace App.Services
{
    public class LocationService : ILocationService
    {
        private CancellationTokenSource? _cts;

        public async Task BatDauTheoDoiAsync(Action<double, double> khiCoViTri)
        {
            // Xin quyền GPS
            var trangThai = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (trangThai != PermissionStatus.Granted)
                throw new Exception("Không có quyền truy cập GPS.");

            _cts = new CancellationTokenSource();

            // Chạy vòng lặp theo dõi trên luồng nền
            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var viTri = await Geolocation.GetLocationAsync(
                            new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)),
                            _cts.Token
                        );

                        if (viTri != null)
                            khiCoViTri(viTri.Latitude, viTri.Longitude);
                    }
                    catch (FeatureNotEnabledException)
                    {
                        // GPS tắt — thông báo cho người dùng nếu cần
                    }
                    catch (Exception) { /* bỏ qua lỗi tạm thời */ }

                    await Task.Delay(5000, _cts.Token); // kiểm tra mỗi 5 giây
                }
            }, _cts.Token);
        }

        public void DungTheoDoi() => _cts?.Cancel();

        public async Task<(double Lat, double Lng)?> LayViTriHienTaiAsync()
        {
            var viTri = await Geolocation.GetLastKnownLocationAsync();
            if (viTri == null) return null;
            return (viTri.Latitude, viTri.Longitude);
        }
    }
}