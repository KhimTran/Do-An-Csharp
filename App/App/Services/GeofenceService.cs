using App.Models;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class GeofenceService
    {
        private static readonly TimeSpan CooldownMacDinh = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CuaSoLamMoiDanhSachPoi = TimeSpan.FromSeconds(30);

        private readonly LocalDatabase _db;

        // Lưu POI đang ở trong vùng để chống trigger lặp lại mỗi lần GPS cập nhật.
        private readonly HashSet<int> _dangTrongVung = new();

        // Cooldown theo bộ nhớ để giảm truy vấn SQLite liên tục khi đi gần 1 điểm.
        private readonly Dictionary<int, DateTime> _lanKichHoatCuoi = new();

        // Cache POI ngắn hạn để giảm tần suất đọc SQLite (GPS cập nhật khá dày, khoảng 3 giây/lần).
        private List<PoiModel> _cachedPoi = new();
        private DateTime _lanLamMoiPoiCuoi = DateTime.MinValue;

        public GeofenceService(LocalDatabase db) => _db = db;

        /// <summary>
        /// Gọi mỗi khi GPS cập nhật. Trả về POI cần phát thuyết minh, hoặc null.
        /// </summary>
        public async Task<PoiModel?> KiemTraVungAsync(double latNguoiDung, double lngNguoiDung)
        {
            var danhSach = await LayDanhSachPoiCoCacheAsync();

            // Lấy bán kính từ Settings (Người B tuần 4), mặc định 100m.
            int banKinhMacDinh = Preferences.Get("geofence_radius", 100);

            foreach (var poi in danhSach)
            {
                double khoangCach = TinhKhoangCachMetres(
                    latNguoiDung, lngNguoiDung,
                    poi.Lat, poi.Lng
                );

                // Dùng bán kính lớn hơn giữa bán kính POI và bán kính từ Settings.
                double banKinhApDung = Math.Max(poi.BanKinh, banKinhMacDinh);
                bool dangNamTrongVung = khoangCach <= banKinhApDung;

                if (!dangNamTrongVung)
                {
                    // Hysteresis: phải ra khỏi 1.5x bán kính mới cho phép vào lại.
                    if (khoangCach > banKinhApDung * 1.5)
                        _dangTrongVung.Remove(poi.Id);

                    continue;
                }

                // Nếu vẫn đứng trong vùng cũ thì bỏ qua để tránh bắn event liên tục.
                if (_dangTrongVung.Contains(poi.Id))
                    continue;

                if (!DaQuaCooldownBoNho(poi.Id))
                    continue;

                // Kiểm tra cooldown từ SQLite (bền vững qua restart app).
                bool duocPhepTheoSQLite = await _db.KiemTraCooldownAsync(poi.Id);
                if (!duocPhepTheoSQLite)
                {
                    // Đồng bộ cache bộ nhớ để lần kế tiếp không cần đụng DB sớm.
                    _lanKichHoatCuoi[poi.Id] = DateTime.Now;
                    continue;
                }

                _dangTrongVung.Add(poi.Id);
                _lanKichHoatCuoi[poi.Id] = DateTime.Now;

                // Ghi lịch sử vào SQLite.
                await _db.GhiLichSuPhatAsync(new LichSuPhatModel
                {
                    PoiId = poi.Id,
                    TenPoi = poi.Ten,
                    NgonNgu = "vi",
                    ThoiGianPhat = DateTime.Now,
                    NguonKichHoat = "GPS"
                });

                return poi;
            }

            return null;
        }

        private bool DaQuaCooldownBoNho(int poiId)
        {
            if (!_lanKichHoatCuoi.TryGetValue(poiId, out var lanCuoi))
                return true;

            return DateTime.Now - lanCuoi >= CooldownMacDinh;
        }

        private async Task<List<PoiModel>> LayDanhSachPoiCoCacheAsync()
        {
            bool canLamMoi = _cachedPoi.Count == 0 || DateTime.Now - _lanLamMoiPoiCuoi >= CuaSoLamMoiDanhSachPoi;
            if (!canLamMoi)
                return _cachedPoi;

            _cachedPoi = await _db.LayTatCaPoiAsync();
            _lanLamMoiPoiCuoi = DateTime.Now;
            return _cachedPoi;
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
    }
}
