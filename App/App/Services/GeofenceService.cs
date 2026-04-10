using App.Models;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class GeofenceService
    {
        private readonly LocalDatabase _db;

        public GeofenceService(LocalDatabase db) => _db = db;

        /// <summary>
        /// Gọi mỗi khi GPS cập nhật. Trả về POI cần phát thuyết minh, hoặc null.
        /// </summary>
        public async Task<PoiModel?> KiemTraVungAsync(double latNguoiDung, double lngNguoiDung)
        {
            var danhSach = await _db.LayTatCaPoiAsync();

            // Lấy bán kính từ Settings, mặc định 100m
            int banKinhMacDinh = Preferences.Get("geofence_radius", 100);

            foreach (var poi in danhSach)
            {
                double khoangCach = TinhKhoangCachMetres(
                    latNguoiDung, lngNguoiDung,
                    poi.Lat, poi.Lng
                );

                // Dùng bán kính lớn hơn giữa bán kính POI và bán kính từ Settings
                double banKinhApDung = Math.Max(poi.BanKinh, banKinhMacDinh);

                if (khoangCach <= banKinhApDung)
                {
                    // Kiểm tra cooldown từ SQLite
                    bool duocPhep = await _db.KiemTraCooldownAsync(poi.Id);
                    if (!duocPhep) continue;

                    string maNgonNgu = Preferences.Get("tts_language", "vi-VN");

                    // Ghi lịch sử vào SQLite với ngôn ngữ đúng theo settings hiện tại
                    await _db.GhiLichSuPhatAsync(new LichSuPhatModel
                    {
                        PoiId = poi.Id,
                        TenPoi = poi.Ten,
                        NgonNgu = RutGonMaNgonNgu(maNgonNgu),
                        ThoiGianPhat = DateTime.Now,
                        NguonKichHoat = "GPS"
                    });

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
            const double R = 6_371_000;
            double dLat = ToRad(lat2 - lat1);
            double dLng = ToRad(lng2 - lng1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;

        private static string RutGonMaNgonNgu(string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return "en";

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return "zh";

            return "vi";
        }
    }
}