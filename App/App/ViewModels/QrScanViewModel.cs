using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using App.Services;
using App.Models;
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

        [RelayCommand]
        public async Task XuLyQrAsync(string ketQua)
        {
            if (!DangQuet) return;
            DangQuet = false;

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

            // Đọc ngôn ngữ từ Preferences
            string maNgonNgu = Preferences.Get("tts_language", "vi-VN");
            string noiDung = ChonNoiDungTheoNgonNgu(poi, maNgonNgu);

            ThongBao = $"🎯 Đang phát: {poi.Ten}";

            await _db.GhiLichSuPhatAsync(new LichSuPhatModel
            {
                PoiId = poi.Id,
                TenPoi = poi.Ten,
                NgonNgu = RutGonMaNgonNgu(maNgonNgu), // vi / en / zh
                ThoiGianPhat = DateTime.Now,
                NguonKichHoat = "QR"
            });

            await _tts.PhatAmAsync(noiDung, maNgonNgu);

            ThongBao = "✅ Xong! Quét mã khác?";
            await Task.Delay(2000);
            DangQuet = true;
            ThongBao = "Hướng camera vào mã QR";
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