using App.Models;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class GeofenceService
    {
        private readonly LocalDatabase _db;
        private readonly Dictionary<int, DateTime> _lichSuPhat = new();

        public GeofenceService(LocalDatabase db)
        {
            _db = db;
        }

        public async Task<PoiModel?> KiemTraVungAsync(double latNguoiDung, double lngNguoiDung)
        {
            var danhSach = await _db.LayTatCaPoiAsync();
            PoiModel? poiGanNhatHopLe = null;
            double kcNhoNhat = double.MaxValue;

            int banKinhMacDinh = Preferences.Get("geofence_radius", 100);

            foreach (var poi in danhSach)
            {
                double khoangCach = TinhKhoangCachMetres(
                    latNguoiDung, lngNguoiDung,
                    poi.Lat, poi.Lng);

                double banKinhApDung = banKinhMacDinh;

                if (khoangCach <= banKinhApDung)
                {
                    if (_lichSuPhat.TryGetValue(poi.Id, out DateTime lanPhatCuoi))
                    {
                        if ((DateTime.Now - lanPhatCuoi).TotalSeconds < 10)
                            continue;
                    }

                    if (khoangCach < kcNhoNhat)
                    {
                        kcNhoNhat = khoangCach;
                        poiGanNhatHopLe = poi;
                    }
                }
            }

            if (poiGanNhatHopLe != null)
            {
                _lichSuPhat[poiGanNhatHopLe.Id] = DateTime.Now;
            }

            return poiGanNhatHopLe;
        }

        public static double TinhKhoangCachMetres(
            double lat1, double lng1,
            double lat2, double lng2)
        {
            const double R = 6_371_000;

            double dLat = ToRad(lat2 - lat1);
            double dLng = ToRad(lng2 - lng1);

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}