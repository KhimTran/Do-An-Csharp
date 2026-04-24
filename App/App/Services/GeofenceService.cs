using App.Models;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class GeofenceService
    {
#if DEBUG
        private static readonly TimeSpan CooldownMacDinh = TimeSpan.FromSeconds(20);
#else
        private static readonly TimeSpan CooldownMacDinh = TimeSpan.FromMinutes(5);
#endif
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

        public enum GeofenceCheckStatus
        {
            NoPoi,
            OutsideZone,
            Cooldown,
            Triggered
        }

        public sealed record GeofenceCheckResult(
            GeofenceCheckStatus Status,
            PoiModel? Poi,
            PoiModel? NearestPoi,
            double DistanceMeters,
            double RadiusMeters,
            string Reason);

        /// <summary>
        /// Gọi mỗi khi GPS cập nhật. Trả về POI cần phát thuyết minh, hoặc null.
        /// </summary>
        public async Task<PoiModel?> KiemTraVungAsync(double latNguoiDung, double lngNguoiDung)
        {
            var result = await KiemTraVungChiTietAsync(latNguoiDung, lngNguoiDung);
            return result.Status == GeofenceCheckStatus.Triggered ? result.Poi : null;
        }

        public async Task<GeofenceCheckResult> KiemTraVungChiTietAsync(double latNguoiDung, double lngNguoiDung)
        {
            var danhSach = await LayDanhSachPoiCoCacheAsync();
            if (danhSach.Count == 0)
            {
                return new GeofenceCheckResult(
                    GeofenceCheckStatus.NoPoi,
                    null,
                    null,
                    double.MaxValue,
                    0,
                    "no-poi");
            }

            int banKinhMacDinh = Math.Max(Preferences.Get("geofence_radius", 50), 30);

            var ketQua = danhSach
                .Select(poi =>
                {
                    double khoangCach = TinhKhoangCachMetres(
                        latNguoiDung,
                        lngNguoiDung,
                        poi.Lat,
                        poi.Lng);

                    double banKinhApDung = Math.Max(poi.BanKinh, banKinhMacDinh);

                    return new
                    {
                        Poi = poi,
                        KhoangCach = khoangCach,
                        BanKinh = banKinhApDung,
                        TrongVung = khoangCach <= (banKinhApDung + 15)
                    };
                })
                .OrderBy(x => x.KhoangCach)
                .ThenBy(x => x.Poi.UuTien)
                .ToList();

            var ganNhat = ketQua.FirstOrDefault();
            if (ganNhat == null)
            {
                return new GeofenceCheckResult(
                    GeofenceCheckStatus.NoPoi,
                    null,
                    null,
                    double.MaxValue,
                    0,
                    "no-poi");
            }

            foreach (var item in ketQua)
            {
                if (item.KhoangCach > item.BanKinh * 1.5)
                    _dangTrongVung.Remove(item.Poi.Id);
            }

            var danhSachTrongVung = ketQua
                .Where(x => x.TrongVung)
                .OrderBy(x => x.KhoangCach)
                .ThenBy(x => x.Poi.UuTien)
                .ToList();

            if (danhSachTrongVung.Count == 0)
            {
                return new GeofenceCheckResult(
                    GeofenceCheckStatus.OutsideZone,
                    null,
                    ganNhat.Poi,
                    ganNhat.KhoangCach,
                    ganNhat.BanKinh,
                    "outside-zone");
            }

            var mucTieu = danhSachTrongVung.First();
            var poi = mucTieu.Poi;

            var lanPhatGanNhat = await _db.LayLanPhatGpsGanNhatAsync(poi.Id);
            if (lanPhatGanNhat.HasValue && DateTime.Now - lanPhatGanNhat.Value < CooldownMacDinh)
            {
                _dangTrongVung.Add(poi.Id);
                _lanKichHoatCuoi[poi.Id] = lanPhatGanNhat.Value;

                return new GeofenceCheckResult(
                    GeofenceCheckStatus.Cooldown,
                    poi,
                    ganNhat.Poi,
                    mucTieu.KhoangCach,
                    mucTieu.BanKinh,
                    "best-poi-sqlite-cooldown");
            }

            if (!DaQuaCooldownBoNho(poi.Id))
            {
                return new GeofenceCheckResult(
                    GeofenceCheckStatus.Cooldown,
                    poi,
                    ganNhat.Poi,
                    mucTieu.KhoangCach,
                    mucTieu.BanKinh,
                    "best-poi-memory-cooldown");
            }

            _dangTrongVung.Add(poi.Id);
            _lanKichHoatCuoi[poi.Id] = DateTime.Now;

            return new GeofenceCheckResult(
                GeofenceCheckStatus.Triggered,
                poi,
                ganNhat.Poi,
                mucTieu.KhoangCach,
                mucTieu.BanKinh,
                "best-poi-triggered");
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

        public void HoanTacLanKichHoat(int poiId)
        {
            _dangTrongVung.Remove(poiId);
            _lanKichHoatCuoi.Remove(poiId);
        }

        public void ResetTrangThaiTamThoi()
        {
            _dangTrongVung.Clear();
            _lanKichHoatCuoi.Clear();
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
