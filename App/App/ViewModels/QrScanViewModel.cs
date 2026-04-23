using App.Models;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace App.ViewModels
{
    public sealed class QrAlertRequestedEventArgs : EventArgs
    {
        public QrAlertRequestedEventArgs(string title, string message)
        {
            Title = title;
            Message = message;
        }

        public string Title { get; }
        public string Message { get; }
    }

    public partial class QrScanViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;
        private readonly ITtsService _tts;
        private readonly AnalyticsService _analytics;
        private readonly SyncService _sync;

        private string _maVuaQuet = string.Empty;
        private DateTime _thoiDiemQuetCuoi = DateTime.MinValue;

        public event EventHandler<QrAlertRequestedEventArgs>? AlertRequested;

        public QrScanViewModel(LocalDatabase db, ITtsService tts, AnalyticsService analytics, SyncService sync)
        {
            _db = db;
            _tts = tts;
            _analytics = analytics;
            _sync = sync;
            ThongBao = LocalizationResourceManager.Instance["QrPage_AimCamera"];
        }

        [ObservableProperty] private bool dangQuet = true;
        [ObservableProperty] private string thongBao = string.Empty;

        [RelayCommand]
        public async Task XuLyQrAsync(string ketQua)
        {
            if (!DangQuet || string.IsNullOrWhiteSpace(ketQua))
                return;

            if (_maVuaQuet == ketQua && (DateTime.Now - _thoiDiemQuetCuoi).TotalSeconds < 3)
                return;

            _maVuaQuet = ketQua;
            _thoiDiemQuetCuoi = DateTime.Now;
            DangQuet = false;

            try
            {
                var payload = QrScanPayloadParser.Parse(ketQua);
                bool daXuLyBootstrapQr = false;

                // QR bootstrap co server/ngrok URL thi luu Base URL va sync ngay.
                if (!string.IsNullOrWhiteSpace(payload.ApiBaseUrlCandidate))
                {
                    await XuLyQrCauHinhServerAsync(payload.ApiBaseUrlCandidate);
                    daXuLyBootstrapQr = true;
                }

                // QR bootstrap co link APK thi mo trinh duyet de tai/cai dat app.
                if (!string.IsNullOrWhiteSpace(payload.ApkUrl))
                {
                    await MoLinkApkAsync(payload.ApkUrl, daXuLyBootstrapQr);
                    daXuLyBootstrapQr = true;
                }

                if (daXuLyBootstrapQr)
                {
                    await MoLaiCheDoQuetSauDelay();
                    return;
                }

                if (!payload.PoiId.HasValue)
                {
                    ThongBao = LocalizationResourceManager.Instance.Translate("QrPage_Invalid", ketQua);
                    await MoLaiCheDoQuetSauDelay();
                    return;
                }

                await DocPoiTuQrAsync(payload.PoiId.Value);
                await MoLaiCheDoQuetSauDelay();
            }
            catch (Exception ex)
            {
                ThongBao = $"Khong xu ly duoc QR: {ex.Message}";
                AlertRequested?.Invoke(this, new QrAlertRequestedEventArgs("Loi QR", ex.Message));
                await MoLaiCheDoQuetSauDelay();
            }
        }

        private async Task XuLyQrCauHinhServerAsync(string serverUrlFromQr)
        {
            var result = await _sync.CapNhatApiBaseUrlTuQrAsync(serverUrlFromQr);
            ThongBao = result.Message;

            if (!result.IsValid)
            {
                AlertRequested?.Invoke(this, new QrAlertRequestedEventArgs("QR ngrok khong hop le", result.Message));
            }
        }

        private async Task MoLinkApkAsync(string apkUrl, bool daXuLyServerUrl)
        {
            try
            {
                var uri = new Uri(apkUrl, UriKind.Absolute);
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
                ThongBao = daXuLyServerUrl
                    ? $"Da cap nhat server. Dang mo link cai dat: {uri}"
                    : $"Dang mo link cai dat: {uri}";
            }
            catch (Exception ex)
            {
                ThongBao = $"Khong mo duoc link APK: {ex.Message}";
                AlertRequested?.Invoke(this, new QrAlertRequestedEventArgs("Khong mo duoc APK", ex.Message));
            }
        }

        private async Task DocPoiTuQrAsync(int poiId)
        {
            var poi = await _db.LayPoiTheoIdAsync(poiId);
            if (poi == null)
            {
                ThongBao = LocalizationResourceManager.Instance.Translate("QrPage_PoiNotFound", poiId);
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

            string khoaAmThanh = $"poi:{poi.Id}:{RutGonMaNgonNgu(maNgonNgu)}";
            var ketQuaPhat = await _tts.PhatAmAsync(noiDung, maNgonNgu, khoaAmThanh);

            if (ketQuaPhat.Completed && ketQuaPhat.CreatedNewSession)
            {
                int thoiLuongGiay = AnalyticsService.UocTinhThoiLuongGiay(noiDung);
                await _analytics.GuiLogAsync(poi.Id, poi.Ten, "QR", thoiLuongGiay);
            }

            ThongBao = LocalizationResourceManager.Instance["QrPage_Done"];
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
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return "en";

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return "zh";

            return "vi";
        }
    }
}
