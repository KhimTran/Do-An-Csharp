using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using App.Services;
using Microsoft.Maui.Storage;

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

            string maNgonNgu = LayNgonNguTuPreferences();
            string noiDung = ChonNoiDungTheoNgonNgu(poi, maNgonNgu);

            // QR bỏ qua cooldown (BR-002)
            ThongBao = $"🎯 Đang phát: {poi.Ten}";
            await _db.GhiLichSuPhatAsync(new Models.LichSuPhatModel
            {
                PoiId = poi.Id,
                TenPoi = poi.Ten,
                NgonNgu = LayMaNgonNguNgan(maNgonNgu),
                ThoiGianPhat = DateTime.Now,
                NguonKichHoat = "QR" // Đánh dấu nguồn là QR
            });
            await _tts.PhatAmAsync(noiDung, maNgonNgu);

            ThongBao = "✅ Xong! Quét mã khác?";
            await Task.Delay(2000);
            DangQuet = true;
            ThongBao = "Hướng camera vào mã QR";
        }

        private static string LayNgonNguTuPreferences()
        {
            var ngonNguMoi = Preferences.Get("ngon_ngu", string.Empty);
            if (!string.IsNullOrWhiteSpace(ngonNguMoi))
                return ngonNguMoi;

            return Preferences.Get("tts_language", "vi-VN");
        }

        private static string LayMaNgonNguNgan(string maNgonNgu)
        {
            if (string.IsNullOrWhiteSpace(maNgonNgu)) return "vi";
            return maNgonNgu.Split('-')[0].ToLowerInvariant();
        }

        private static string ChonNoiDungTheoNgonNgu(Models.PoiModel poi, string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(poi.MoTa_En) ? poi.MoTa_Vi : poi.MoTa_En;

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(poi.MoTa_Zh) ? poi.MoTa_Vi : poi.MoTa_Zh;

            return poi.MoTa_Vi;
        }
    }
}
