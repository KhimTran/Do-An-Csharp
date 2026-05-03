using App.Models;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private const int ThoiGianChanQuetLapGiay = 4;
        private const string EmptyDescriptionMessage = "Chưa có nội dung thuyết minh.";

        private readonly LocalDatabase _db;
        private readonly INarrationService _narration;
        private readonly AnalyticsService _analytics;
        private readonly SyncService _sync;

        private string _maVuaQuet = string.Empty;
        private DateTime _thoiDiemQuetCuoi = DateTime.MinValue;

        public event EventHandler<QrAlertRequestedEventArgs>? AlertRequested;

        public QrScanViewModel(LocalDatabase db, INarrationService narration, AnalyticsService analytics, SyncService sync)
        {
            _db = db;
            _narration = narration;
            _analytics = analytics;
            _sync = sync;
        }

        [ObservableProperty] private bool dangQuet = true;
        [ObservableProperty] private string trangThaiQuet = "Sẵn sàng quét mã QR";

        [RelayCommand]
        public async Task XuLyQrAsync(string ketQua)
        {
            if (!DangQuet || string.IsNullOrWhiteSpace(ketQua))
                return;

            var thoiDiemHienTai = DateTime.UtcNow;
            if (_maVuaQuet == ketQua &&
                (thoiDiemHienTai - _thoiDiemQuetCuoi).TotalSeconds < ThoiGianChanQuetLapGiay)
            {
                return;
            }

            _maVuaQuet = ketQua;
            _thoiDiemQuetCuoi = thoiDiemHienTai;
            DangQuet = false;

            try
            {
                var payload = QrScanPayloadParser.Parse(ketQua);

                if (!payload.PoiId.HasValue)
                {
                    TrangThaiQuet = "Mã QR không hợp lệ";
                    await MoLaiCheDoQuetSauDelay();
                    return;
                }

                await DocPoiTuQrAsync(payload.PoiId.Value);
                await MoLaiCheDoQuetSauDelay();
            }
            catch (Exception ex)
            {
                TrangThaiQuet = "Mã QR không hợp lệ";
                AlertRequested?.Invoke(this, new QrAlertRequestedEventArgs("Lỗi QR", ex.Message));
                await MoLaiCheDoQuetSauDelay();
            }
        }

        private async Task DocPoiTuQrAsync(int poiId)
        {
            try
            {
                await _sync.DongBoPoisAsync();
            }
            catch
            {
                // Vẫn ưu tiên dữ liệu local để trang QR dùng được khi mạng lỗi.
            }

            var poi = await _db.LayPoiTheoIdAsync(poiId);
            if (poi == null)
            {
                TrangThaiQuet = "Không tìm thấy địa điểm này";
                return;
            }

            string maNgonNgu = Preferences.Get("tts_language", "vi-VN");

            TrangThaiQuet = $"Đang phát thuyết minh: {poi.Ten}";

            var ketQuaPhat = await _narration.PhatThuyetMinhPoiAsync(poi, maNgonNgu);

            if (!ketQuaPhat.Completed)
            {
                TrangThaiQuet = ketQuaPhat.Status == "empty"
                    ? EmptyDescriptionMessage
                    : $"Không thể phát thuyết minh: {poi.Ten}";
                return;
            }

            await _db.GhiLichSuPhatAsync(new LichSuPhatModel
            {
                PoiId = poi.Id,
                TenPoi = poi.Ten,
                NgonNgu = ketQuaPhat.Language,
                ThoiGianPhat = DateTime.Now,
                NguonKichHoat = "QR"
            });

            if (ketQuaPhat.CreatedNewSession)
            {
                int thoiLuongGiay = AnalyticsService.UocTinhThoiLuongGiay(ketQuaPhat.TextForAnalytics);
                await _analytics.GuiLogAsync(poi.Id, poi.Ten, "QR", thoiLuongGiay);
            }

            TrangThaiQuet = $"Đã phát xong thuyết minh: {poi.Ten}";
        }

        private async Task MoLaiCheDoQuetSauDelay()
        {
            await Task.Delay(1200);
            DangQuet = true;
        }

    }
}
