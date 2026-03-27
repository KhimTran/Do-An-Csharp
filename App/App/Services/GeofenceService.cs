using App.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Services
{
    public class GeofenceService
    {
        private readonly LocalDatabase _db;

        // Dictionary lưu thời gian đã phát của từng POI (Key là ID của điểm, Value là thời gian phát)
        // Dùng để làm tính năng Cooldown 5 phút (chống phát âm liên tục)
        private readonly Dictionary<int, DateTime> _lichSuPhat = new();

        public GeofenceService(LocalDatabase db) => _db = db;

        /// <summary>
        /// Gọi mỗi khi GPS cập nhật. Trả về POI đầu tiên người dùng đang đứng trong vùng,
        /// hoặc null nếu không có.
        /// </summary>
        public async Task<PoiModel?> KiemTraVungAsync(double latNguoiDung, double lngNguoiDung)
        {
            var danhSach = await _db.LayTatCaPoiAsync();

            foreach (var poi in danhSach)
            {
                double khoangCach = TinhKhoangCachMetres(
                    latNguoiDung, lngNguoiDung,
                    poi.Lat, poi.Lng
                );

                // Nếu người dùng lọt vào vùng bán kính
                if (khoangCach <= poi.BanKinh)
                {
                    // Kiểm tra Cooldown 5 phút
                    if (_lichSuPhat.TryGetValue(poi.Id, out DateTime lanPhatCuoi))
                    {
                        // Nếu khoảng thời gian từ lần phát cuối đến hiện tại < 5 phút
                        if ((DateTime.Now - lanPhatCuoi).TotalMinutes < 5)
                        {
                            continue; // Bỏ qua, chưa đủ thời gian cooldown, không phát lại
                        }
                    }

                    // Đánh dấu thời gian phát mới nhất và trả về POI này để kích hoạt âm thanh
                    _lichSuPhat[poi.Id] = DateTime.Now;
                    return poi;
                }
            }

            return null;
        }

        /// <summary>
        /// Công thức Haversine — tính khoảng cách giữa 2 toạ độ (đơn vị: mét)
        /// </summary>
        public static double TinhKhoangCachMetres(
            double lat1, double lng1,
            double lat2, double lng2)
        {
            const double R = 6_371_000; // bán kính Trái Đất (mét)

            double dLat = ToRad(lat2 - lat1);
            double dLng = ToRad(lng2 - lng1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}