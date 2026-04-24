using App.Models;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;

namespace App.ViewModels
{
    public partial class HistoryTopPoiItemViewModel : ObservableObject
    {
        public int Hang { get; init; }
        public string TenPoi { get; init; } = string.Empty;
        public int SoLanNghe { get; init; }
        public bool LaTop1 { get; init; }
        public string NhanHang => $"#{Hang}";
        public string LuotNgheText => $"{SoLanNghe} lượt nghe";
        public string VienMau => LaTop1 ? "#FFC247" : "#3A3550";
        public string NenMau => LaTop1 ? "#2B2144" : "#19172A";
        public string NhanNenMau => LaTop1 ? "#F59E0B" : "#6D5CD4";
    }

    public partial class HistoryRecentItemViewModel : ObservableObject
    {
        public int PoiId { get; init; }
        public string TenPoi { get; init; } = string.Empty;
        public string NgonNgu { get; init; } = "vi";
        public DateTime ThoiGianPhat { get; init; }
        public string NguonKichHoat { get; init; } = "GPS";
        public string GioPhat => ThoiGianPhat.ToLocalTime().ToString("HH:mm");
        public string NguonHienThi => string.Equals(NguonKichHoat, "QR", StringComparison.OrdinalIgnoreCase) ? "QR" : "GPS";
        public string ChuCaiDaiDien => string.IsNullOrWhiteSpace(TenPoi) ? "PO" : TenPoi[..Math.Min(2, TenPoi.Length)].ToUpperInvariant();
        public string MauDaiDien => string.Equals(NguonKichHoat, "QR", StringComparison.OrdinalIgnoreCase) ? "#7C3AED" : "#5B21B6";
    }

    public partial class HistoryViewModel : ObservableObject
    {
        private readonly LocalDatabase _db;
        private readonly ITtsService _tts;
        private readonly AnalyticsService _analytics;

        public ObservableCollection<HistoryRecentItemViewModel> LichSuGanDay { get; } = new();
        public ObservableCollection<HistoryTopPoiItemViewModel> TopPoi { get; } = new();

        [ObservableProperty] private bool dangTai;
        [ObservableProperty] private int tongLuotNghe;
        [ObservableProperty] private int tongDiaDiemDaKhamPha;
        [ObservableProperty] private string? tenPoiNgheNhieuNhat;
        [ObservableProperty] private string tomTatHanhTrinh = string.Empty;

        public HistoryViewModel(LocalDatabase db, ITtsService tts, AnalyticsService analytics)
        {
            _db = db;
            _tts = tts;
            _analytics = analytics;
        }

        [RelayCommand]
        public async Task TaiDuLieuAsync()
        {
            if (DangTai)
                return;

            try
            {
                DangTai = true;

                var lichSu = await _db.LayLichSuMoiNhatAsync();
                var topPoi = await _db.LayTopPoiDuocNgheNhieuAsync();

                TongLuotNghe = topPoi.Sum(x => x.SoLanNghe);
                TongDiaDiemDaKhamPha = topPoi.Count;
                TenPoiNgheNhieuNhat = topPoi.FirstOrDefault()?.TenPoi ?? "Chưa có";
                TomTatHanhTrinh = LocalizationResourceManager.Instance.Translate(
                    "HistoryPage_Summary",
                    TongDiaDiemDaKhamPha,
                    TongLuotNghe);

                LichSuGanDay.Clear();
                foreach (var item in lichSu)
                {
                    LichSuGanDay.Add(new HistoryRecentItemViewModel
                    {
                        PoiId = item.PoiId,
                        TenPoi = item.TenPoi,
                        NgonNgu = item.NgonNgu,
                        ThoiGianPhat = item.ThoiGianPhat,
                        NguonKichHoat = item.NguonKichHoat
                    });
                }

                TopPoi.Clear();
                for (int i = 0; i < topPoi.Count; i++)
                {
                    var item = topPoi[i];
                    TopPoi.Add(new HistoryTopPoiItemViewModel
                    {
                        Hang = i + 1,
                        TenPoi = item.TenPoi,
                        SoLanNghe = item.SoLanNghe,
                        LaTop1 = i == 0
                    });
                }
            }
            finally
            {
                DangTai = false;
            }
        }

        [RelayCommand]
        private async Task NgheLaiAsync(HistoryRecentItemViewModel? item)
        {
            if (item == null)
                return;

            var poi = await _db.LayPoiTheoIdAsync(item.PoiId);
            if (poi == null)
                return;

            string maNgonNgu = ChuyenMaNgonNgu(item.NgonNgu);
            string noiDung = ChonNoiDungTheoNgonNgu(poi, maNgonNgu);
            if (string.IsNullOrWhiteSpace(noiDung))
                return;

            string khoaNoiDung = StringComparer.Ordinal.GetHashCode(noiDung.Trim()).ToString("X");
            string khoaAmThanh = $"poi:{poi.Id}:{RutGonMaNgonNgu(maNgonNgu)}:{khoaNoiDung}";
            var ketQuaPhat = await _tts.PhatAmAsync(noiDung, maNgonNgu, khoaAmThanh, poi.Ten);
            if (!ketQuaPhat.Completed || !ketQuaPhat.CreatedNewSession)
                return;

            await _db.GhiLichSuPhatAsync(new LichSuPhatModel
            {
                PoiId = poi.Id,
                TenPoi = poi.Ten,
                NgonNgu = RutGonMaNgonNgu(maNgonNgu),
                ThoiGianPhat = DateTime.Now,
                NguonKichHoat = item.NguonKichHoat
            });

            int thoiLuongGiay = AnalyticsService.UocTinhThoiLuongGiay(noiDung);
            await _analytics.GuiLogAsync(poi.Id, poi.Ten, item.NguonKichHoat, thoiLuongGiay);

            await TaiDuLieuAsync();
        }

        private static string ChonNoiDungTheoNgonNgu(PoiModel poi, string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(poi.MoTa_En) ? poi.MoTa_Vi : poi.MoTa_En;

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(poi.MoTa_Zh) ? poi.MoTa_Vi : poi.MoTa_Zh;

            return poi.MoTa_Vi;
        }

        private static string ChuyenMaNgonNgu(string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return "en-US";

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return "zh-CN";

            return Preferences.Get("tts_language", "vi-VN");
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
