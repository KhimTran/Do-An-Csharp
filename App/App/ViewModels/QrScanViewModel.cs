using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using App.Services;

namespace App.ViewModels
{
    public partial class QrScanViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;
        private readonly ITtsService _tts;

        public QrScanViewModel(LocalDatabase db, ITtsService tts)
        {
            _db = db;
            _tts = tts;
        }

        [ObservableProperty] private bool dangQuet = true;
        [ObservableProperty] private string thongBao = "Hướng camera vào mã QR";

        // Gọi khi ZXing quét được mã
        [RelayCommand]
        public async Task XuLyQrAsync(string ketQua)
        {
            if (!DangQuet) return; // Chống quét trùng lặp
            DangQuet = false;

            // Mã QR chứa số ID của POI, ví dụ: "1" hoặc "2"
            if (!int.TryParse(ketQua, out int poiId))
            {
                ThongBao = $"Mã QR không hợp lệ: {ketQua}";
                DangQuet = true;
                return;
            }

            var poi = await _db.LayPoiTheoIdAsync(poiId);
            if (poi == null)
            {
                ThongBao = $"Không tìm thấy điểm số {poiId}";
                DangQuet = true;
                return;
            }

            // QR bỏ qua cooldown (BR-002)
            ThongBao = $"🎯 Đang phát: {poi.Ten}";
            await _db.GhiLichSuPhatAsync(new Models.LichSuPhatModel
            {
                PoiId = poi.Id,
                TenPoi = poi.Ten,
                NgonNgu = "vi",
                ThoiGianPhat = DateTime.Now,
                NguonKichHoat = "QR" // Đánh dấu nguồn là QR
            });
            await _tts.PhatAmAsync(poi.MoTa_Vi, "vi-VN");

            ThongBao = "✅ Xong! Quét mã khác?";
            await Task.Delay(2000);
            DangQuet = true;
            ThongBao = "Hướng camera vào mã QR";
        }
    }
}