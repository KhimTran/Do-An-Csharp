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
        private readonly AnalyticsService _analytics;

        private string _maVuaQuet = string.Empty;
        private DateTime _thoiDiemQuetCuoi = DateTime.MinValue;

        public QrScanViewModel(LocalDatabase db, ITtsService tts, AnalyticsService analytics)
        {
            _db = db;
            _tts = tts;
            _analytics = analytics;
            ThongBao = LocalizationResourceManager.Instance["QrPage_AimCamera"];
        }

        [ObservableProperty] private bool dangQuet = true;
        [ObservableProperty] private string thongBao = string.Empty;

        [RelayCommand]
        public async Task XuLyQrAsync(string ketQua)
        {
            if (!DangQuet || string.IsNullOrWhiteSpace(ketQua)) return;

            if (_maVuaQuet == ketQua && (DateTime.Now - _thoiDiemQuetCuoi).TotalSeconds < 3)
                return;

            _maVuaQuet = ketQua;
            _thoiDiemQuetCuoi = DateTime.Now;
            DangQuet = false;

            if (!int.TryParse(ketQua, out int poiId))
            {
                ThongBao = LocalizationResourceManager.Instance.Translate("QrPage_Invalid", ketQua);
                await MoLaiCheDoQuetSauDelay();
                return;
            }

            var poi = await _db.LayPoiTheoIdAsync(poiId);
            if (poi == null)
            {
                ThongBao = LocalizationResourceManager.Instance.Translate("QrPage_PoiNotFound", poiId);
                await MoLaiCheDoQuetSauDelay();
                return;
            }

            string maNgonNgu = Preferences.Get("tts_language", "vi-VN");
            string noiDung = ChonNoiDungTheoNgonNgu(poi, maNgonNgu);

            ThongBao = LocalizationResourceManager.Instance.Translate("QrPage_Playing", poi.Ten);

            await _db.GhiLichSuPhatAsync(new LichSuPhatModel
            {
                PoiId = poi.Id,
                TenPoi = poi.Ten,
                NgonNgu = RutGonMaNgonNgu(maNgonNgu),
                ThoiGianPhat = DateTime.Now,
                NguonKichHoat = "QR"
            });

            await _tts.PhatAmAsync(noiDung, maNgonNgu);

            int thoiLuongGiay = AnalyticsService.UocTinhThoiLuongGiay(noiDung);
            await _analytics.GuiLogAsync(poi.Id, poi.Ten, "QR", thoiLuongGiay);

            ThongBao = LocalizationResourceManager.Instance["QrPage_Done"];
            await MoLaiCheDoQuetSauDelay();
        }

        private async Task MoLaiCheDoQuetSauDelay()
        {
            await Task.Delay(1200);
            DangQuet = true;
            ThongBao = LocalizationResourceManager.Instance["QrPage_AimCamera"];
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
