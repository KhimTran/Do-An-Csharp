using CommunityToolkit.Mvvm.ComponentModel;
using App.Models;
using App.Services;

namespace App.ViewModels
{
    public partial class MapViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;
        private readonly ILocationService _gps;
        private List<PoiModel> _danhSachPoi = new();

        public MapViewModel(LocalDatabase db, ILocationService gps)
        {
            _db = db;
            _gps = gps;
        }

        [ObservableProperty] private string tenPoiGanNhat = "Chưa có điểm gần";
        [ObservableProperty] private double khoangCachGanNhat = 0;
        [ObservableProperty] private bool coPoiGanNhat = false;

        public event Action<List<PoiModel>>? OnDaCoiPoi;
        public event Action<double, double>? OnViTriCapNhat;

        public async Task KhoiDongAsync()
        {
            // Load POI từ SQLite
            _danhSachPoi = await _db.LayTatCaPoiAsync();
            OnDaCoiPoi?.Invoke(_danhSachPoi);

            // Bắt đầu GPS — dùng callback của ILocationService
            await _gps.BatDauTheoDoiAsync((lat, lng) =>
            {
                OnViTriCapNhat?.Invoke(lat, lng);
                XuLyViTriMoi(lat, lng);
            });
        }

        public void DungGps() => _gps.DungTheoDoi();

        private void XuLyViTriMoi(double lat, double lng)
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

            if (ganNhat != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TenPoiGanNhat = ganNhat.Ten;
                    KhoangCachGanNhat = minKc;
                    CoPoiGanNhat = minKc <= ganNhat.BanKinh * 3;
                });
            }
        }
    }
}