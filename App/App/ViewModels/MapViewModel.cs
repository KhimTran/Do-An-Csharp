using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using App.Models;
using App.Services;

namespace App.ViewModels
{
    public partial class MapViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;
        private readonly GpsService _gps;
        private List<PoiModel> _danhSachPoi = new();

        public MapViewModel(LocalDatabase db, GpsService gps)
        {
            _db = db;
            _gps = gps;
        }

        [ObservableProperty]
        private string tenPoiGanNhat = "Chưa có điểm gần";

        [ObservableProperty]
        private double khoangCachGanNhat = 0;

        [ObservableProperty]
        private bool coPoiGanNhat = false;

        // Sự kiện báo MapPage thêm pin lên bản đồ
        public event Action<List<PoiModel>>? OnDaCoiPoi;

        // Sự kiện báo vị trí người dùng thay đổi
        public event Action<Location>? OnViTriCapNhat;

        public async Task KhoiDongAsync()
        {
            // Load POI từ SQLite
            _danhSachPoi = await _db.LayTatCaPoiAsync();
            OnDaCoiPoi?.Invoke(_danhSachPoi);

            // Bắt đầu GPS
            _gps.OnViTriMoi += XuLyViTriMoi;
            _gps.BatDauTracking();
        }

        public void DungGps()
        {
            _gps.OnViTriMoi -= XuLyViTriMoi;
            _gps.DungTracking();
        }

        private void XuLyViTriMoi(Location viTri)
        {
            OnViTriCapNhat?.Invoke(viTri);

            // Tìm POI gần nhất
            PoiModel? ganNhat = null;
            double minKc = double.MaxValue;

            foreach (var poi in _danhSachPoi)
            {
                double kc = GpsService.TinhKhoangCach(
                    viTri.Latitude, viTri.Longitude,
                    poi.Lat, poi.Lng
                );
                if (kc < minKc)
                {
                    minKc = kc;
                    ganNhat = poi;
                }
            }

            if (ganNhat != null)
            {
                TenPoiGanNhat = ganNhat.Ten;
                KhoangCachGanNhat = minKc;
                CoPoiGanNhat = minKc <= ganNhat.BanKinh * 3;
            }
        }
    }
}