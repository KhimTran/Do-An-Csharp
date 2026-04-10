using App.Models;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;

namespace App.ViewModels
{
    public partial class QrScanViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;
        private readonly ITtsService _tts;

        private string _maVuaQuet = string.Empty;
        private DateTime _thoiDiemQuetCuoi = DateTime.MinValue;

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
            if (!DangQuet || string.IsNullOrWhiteSpace(ketQua)) return;

            // Chống đọc lặp cùng 1 mã trong cửa sổ 3 giây.
            if (_maVuaQuet == ketQua && (DateTime.Now - _thoiDiemQuetCuoi).TotalSeconds < 3)
                return;

            _maVuaQuet = ketQua;
            _thoiDiemQuetCuoi = DateTime.Now;
            DangQuet = false;

            if (!int.TryParse(ketQua, out int poiId))
            {
                ThongBao = $"Mã QR không hợp lệ: {ketQua}";
                await MoLaiCheDoQuetSauDelay();
                return;
            }

            var poi = await _db.LayPoiTheoIdAsync(poiId);
            if (poi == null)
            {
                ThongBao = $"Không tìm thấy điểm số {poiId}";
                await MoLaiCheDoQuetSauDelay();
                return;
            }

            string maNgonNgu = Preferences.Get("tts_language", "vi-VN");
            string noiDung = ChonNoiDungTheoNgonNgu(poi, maNgonNgu);

            ThongBao = $"🎯 Đang phát: {poi.Ten}";

            await _db.GhiLichSuPhatAsync(new LichSuPhatModel
            {
                PoiId = poi.Id,
                TenPoi = poi.Ten,
                NgonNgu = RutGonMaNgonNgu(maNgonNgu),
                ThoiGianPhat = DateTime.Now,
                NguonKichHoat = "QR"
            });

            await _tts.PhatAmAsync(noiDung, maNgonNgu);

            ThongBao = "✅ Xong! Quét mã khác?";
            await MoLaiCheDoQuetSauDelay();
        }

        private async Task MoLaiCheDoQuetSauDelay()
        {
            await Task.Delay(1200);
            DangQuet = true;
            ThongBao = "Hướng camera vào mã QR";
        }

        private static string ChonNoiDungTheoNgonNgu(PoiModel poi, string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(poi.MoTa_En) ? poi.MoTa_Vi : poi.MoTa_En;

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(poi.MoTa_Zh) ? poi.MoTa_Vi : poi.MoTa_Zh;

            return poi.MoTa_Vi;
        }

        private static string RutGonMaNgonNgu(string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return "en";
            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh";
            return "vi";
        }
    }
}
