using App.Models;

namespace App.Services
{
    public class GpsService
    {
        // Sự kiện bắn ra mỗi khi có vị trí mới
        public event Action<Location>? OnViTriMoi;

        private CancellationTokenSource? _cts;
        private bool _dangChay = false;

        // Bắt đầu tracking liên tục mỗi 3 giây
        public void BatDauTracking()
        {
            if (_dangChay) return;
            _dangChay = true;
            _cts = new CancellationTokenSource();
            _ = VongLapLayViTri(_cts.Token);
        }

        // Dừng tracking
        public void DungTracking()
        {
            _cts?.Cancel();
            _dangChay = false;
        }

        private async Task VongLapLayViTri(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var viTri = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Best,
                        Timeout = TimeSpan.FromSeconds(5)
                    }, token);

                    if (viTri != null)
                        OnViTriMoi?.Invoke(viTri);
                }
                catch (FeatureNotEnabledException)
                {
                    // GPS bị tắt trên thiết bị
                }
                catch (PermissionException)
                {
                    // Chưa cấp quyền GPS
                    DungTracking();
                    return;
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi tạm thời, thử lại sau
                }

                await Task.Delay(3000, token).ContinueWith(_ => { }); // 3 giây
            }
        }

        // Tính khoảng cách Haversine (đơn vị: mét)
        public static double TinhKhoangCach(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000; // bán kính Trái Đất (mét)
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLng = (lng2 - lng1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                     * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}