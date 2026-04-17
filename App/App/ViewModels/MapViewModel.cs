using App.Models;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace App.ViewModels
{
    public partial class MapViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;
        private readonly SyncService _sync;
        private readonly ILocationService _gps;
        private readonly GeofenceService _geofence;
        private readonly ITtsService _tts;
        private readonly AnalyticsService _analytics;

        private List<PoiModel> _danhSachPoi = new();
        private bool _dangXuLyViTri = false;
        private bool _daKhoiDong = false;

        public MapViewModel(
            LocalDatabase db,
            SyncService sync,
            ILocationService gps,
            GeofenceService geofence,
            ITtsService tts,
            AnalyticsService analytics)
        {
            _db = db;
            _sync = sync;
            _gps = gps;
            _geofence = geofence;
            _tts = tts;
            _analytics = analytics;
        }

        [ObservableProperty]
        private string tenPoiGanNhat = LocalizationResourceManager.Instance["MapPage_NoNearest"];

        [ObservableProperty]
        private double khoangCachGanNhat = 0;

        [ObservableProperty]
        private bool coPoiGanNhat = false;

        public event Action<List<PoiModel>>? OnDaCoiPoi;
        public event Action<double, double>? OnViTriCapNhat;

        public async Task KhoiDongAsync()
        {
            if (_daKhoiDong) return;
            _daKhoiDong = true;

            await _sync.DongBoPoisAsync();
            _danhSachPoi = await _db.LayTatCaPoiAsync();
            OnDaCoiPoi?.Invoke(_danhSachPoi);

            await _gps.BatDauTheoDoiAsync((lat, lng) =>
            {
                OnViTriCapNhat?.Invoke(lat, lng);
                _ = XuLyViTriMoiAsync(lat, lng);
            });
        }

        public void DungGps()
        {
            _gps.DungTheoDoi();
            _daKhoiDong = false;
        }

        private async Task XuLyViTriMoiAsync(double lat, double lng)
        {
            if (_dangXuLyViTri)
                return;

            _dangXuLyViTri = true;

            try
            {
                PoiModel? ganNhat = null;
                double minKc = double.MaxValue;

                foreach (var poi in _danhSachPoi)
                {
                    double kc = GeofenceService.TinhKhoangCachMetres(lat, lng, poi.Lat, poi.Lng);

                    if (kc < minKc)
                    {
                        minKc = kc;
                        ganNhat = poi;
                    }
                }

                int banKinhMacDinh = Preferences.Get("geofence_radius", 100);

                if (ganNhat != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TenPoiGanNhat = ganNhat.Ten;
                        KhoangCachGanNhat = minKc;
                        CoPoiGanNhat = minKc <= banKinhMacDinh * 3;
                    });

                    System.Diagnostics.Debug.WriteLine($"[NEAREST] {ganNhat.Ten} - {minKc:F2}m");
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TenPoiGanNhat = LocalizationResourceManager.Instance["MapPage_NoNearest"];
                        KhoangCachGanNhat = 0;
                        CoPoiGanNhat = false;
                    });
                }

                _ = _analytics.GuiRoutePingAsync(lat, lng, "GPS");

                bool forceReread = Preferences.Get("force_reread_once", false);

                if (forceReread && ganNhat != null && minKc <= banKinhMacDinh)
                {
                    System.Diagnostics.Debug.WriteLine($"[FORCE] Đọc lại 1 lần cho POI: {ganNhat.Ten}");
                    await DocThuyetMinhTheoNgonNguAsync(ganNhat, "GPS");
                    Preferences.Set("force_reread_once", false);
                    return;
                }

                var poiCanPhat = await _geofence.KiemTraVungAsync(lat, lng);

                System.Diagnostics.Debug.WriteLine(
                    poiCanPhat != null
                        ? $"[GEOFENCE] Vào vùng: {poiCanPhat.Ten}"
                        : "[GEOFENCE] Chưa vào vùng");

                if (poiCanPhat != null)
                {
                    await DocThuyetMinhTheoNgonNguAsync(poiCanPhat, "GPS");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapViewModel] Lỗi xử lý vị trí: {ex.Message}");
            }
            finally
            {
                _dangXuLyViTri = false;
            }
        }

        private async Task DocThuyetMinhTheoNgonNguAsync(PoiModel poi, string nguon)
        {
            string maNgonNgu = Preferences.Get("tts_language", "vi-VN");
            string noiDung = ChonNoiDungTheoNgonNgu(poi, maNgonNgu);

            System.Diagnostics.Debug.WriteLine($"[LANG] {maNgonNgu}");
            System.Diagnostics.Debug.WriteLine($"[TEXT] {noiDung}");

            if (!string.IsNullOrWhiteSpace(noiDung))
            {
                System.Diagnostics.Debug.WriteLine($"[TTS] Đang đọc: {noiDung}");
                await _tts.PhatAmAsync(noiDung, maNgonNgu);

                int thoiLuongGiay = AnalyticsService.UocTinhThoiLuongGiay(noiDung);
                await _analytics.GuiLogAsync(poi.Id, poi.Ten, nguon, thoiLuongGiay);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[TTS] Nội dung rỗng nên không đọc");
            }
        }

        private static string ChonNoiDungTheoNgonNgu(PoiModel poi, string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(poi.MoTa_En))
                    return poi.MoTa_En;

                return poi.MoTa_Vi;
            }

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(poi.MoTa_Zh))
                    return poi.MoTa_Zh;

                return poi.MoTa_Vi;
            }

            return poi.MoTa_Vi;
        }
    }
}
