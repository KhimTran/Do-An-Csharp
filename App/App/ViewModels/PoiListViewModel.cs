using System.Collections.ObjectModel;
using App.Models;
using App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace App.ViewModels;

public partial class PoiListViewModel : ObservableObject
{
    private const string EmptyDescriptionMessage = "Chưa có nội dung thuyết minh.";
    private readonly LocalDatabase _db;
    private readonly SyncService _sync;
    private readonly ILocationService _gps;
    private readonly INarrationService _narration;
    private readonly AnalyticsService _analytics;

    private List<PoiModel> _poiGoc = [];
    private LocationSnapshot? _viTriHienTai;
    private bool _daKhoiDongGps;

    public PoiListViewModel(
        LocalDatabase db,
        SyncService sync,
        ILocationService gps,
        INarrationService narration,
        AnalyticsService analytics)
    {
        _db = db;
        _sync = sync;
        _gps = gps;
        _narration = narration;
        _analytics = analytics;
    }

    [ObservableProperty]
    private ObservableCollection<PoiListItemViewModel> danhSachPoi = new();

    [ObservableProperty]
    private bool dangTai;

    [ObservableProperty]
    private string thongBao = string.Empty;

    [ObservableProperty]
    private string moTaKhuVuc = "Bạn đang ở gần phố ẩm thực Vĩnh Khánh";

    [ObservableProperty]
    private string huongDan = "Di chuyển lại gần để nghe thuyết minh tự động hoặc bấm để nghe ngay.";

    [ObservableProperty]
    private string trangThaiViTri = "Đang xác định vị trí của bạn...";

    public async Task KhoiDongAsync()
    {
        await TaiDanhSachPoi();

        if (_daKhoiDongGps)
            return;

        _daKhoiDongGps = true;
        await _gps.BatDauTheoDoiAsync(CapNhatViTriMoi, CapNhatTrangThaiGps);
    }

    public void DungGps()
    {
        _gps.DungTheoDoi();
        _daKhoiDongGps = false;
    }

    [RelayCommand]
    public async Task TaiDanhSachPoi()
    {
        try
        {
            DangTai = true;
            ThongBao = LocalizationResourceManager.Instance["PoiSync_Start"];

            bool daDongBo = await _sync.DongBoPoisAsync();
            _poiGoc = await _db.LayTatCaPoiAsync();

            await MainThread.InvokeOnMainThreadAsync(XayDungDanhSachHienThi);

            ThongBao = daDongBo
                ? LocalizationResourceManager.Instance.Translate("PoiSync_Done", DanhSachPoi.Count)
                : LocalizationResourceManager.Instance.Translate("PoiSync_Offline", DanhSachPoi.Count, _sync.LastError);
        }
        catch (Exception ex)
        {
            ThongBao = LocalizationResourceManager.Instance.Translate("Common_Error", ex.Message);
            TrangThaiViTri = "Không thể cập nhật danh sách lúc này. Ứng dụng sẽ tiếp tục dùng dữ liệu local nếu có.";
        }
        finally
        {
            DangTai = false;
        }
    }

    [RelayCommand]
    private async Task NgheNgayAsync(PoiListItemViewModel? item)
    {
        if (item == null)
            return;

        var poi = _poiGoc.FirstOrDefault(x => x.Id == item.Id);
        if (poi == null)
            return;

        string maNgonNgu = Preferences.Get("tts_language", "vi-VN");
        var ketQuaPhat = await _narration.PhatThuyetMinhPoiAsync(
            poi,
            maNgonNgu,
            priority: NarrationRequestPriority.UserInitiated,
            interruptCurrent: true,
            source: "list");
        if (ketQuaPhat.Status == "empty")
            ThongBao = EmptyDescriptionMessage;

        if (!ketQuaPhat.Completed || !ketQuaPhat.CreatedNewSession)
            return;

        int thoiLuongGiay = AnalyticsService.UocTinhThoiLuongGiay(ketQuaPhat.TextForAnalytics);
        await _analytics.GuiLogAsync(poi.Id, poi.Ten, "LIST", thoiLuongGiay);
    }

    [RelayCommand]
    private Task XemBanDoAsync(PoiListItemViewModel? item)
    {
        if (item == null || item.Id <= 0)
            return Task.CompletedTask;

        Preferences.Set(AppNavigationKeys.PendingMapPoiId, item.Id);
        return Shell.Current?.GoToAsync("//map") ?? Task.CompletedTask;
    }

    private void CapNhatViTriMoi(LocationSnapshot snapshot)
    {
        _viTriHienTai = snapshot;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TrangThaiViTri = "Đã cập nhật khoảng cách theo vị trí hiện tại của bạn.";
            XayDungDanhSachHienThi();
        });
    }

    private void CapNhatTrangThaiGps(LocationTrackingStatus status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TrangThaiViTri = status.State switch
            {
                LocationTrackingState.Tracking => "Khoảng cách đang được cập nhật tự động.",
                LocationTrackingState.Simulated => "Đang dùng vị trí demo để bạn xem luồng trải nghiệm.",
                LocationTrackingState.PermissionDenied => "Chưa có quyền vị trí. Bạn vẫn có thể bấm nghe ngay hoặc xem trên bản đồ.",
                LocationTrackingState.Disabled => "GPS đang tắt. Bạn vẫn có thể xem danh sách và mở bản đồ.",
                LocationTrackingState.Error when !string.IsNullOrWhiteSpace(status.Details) => $"Không lấy được GPS: {status.Details}",
                LocationTrackingState.Error => "Không lấy được GPS lúc này. Danh sách vẫn hoạt động offline.",
                _ => "Đang xác định vị trí của bạn..."
            };
        });
    }

    private void XayDungDanhSachHienThi()
    {
        var items = _poiGoc
            .Select(TaoItemHienThi)
            .OrderBy(item => DoUuTienKhoangCach(item.TrangThaiKhoangCach))
            .ThenBy(item => TinhKhoangCachSapXep(item.Id))
            .ThenBy(item => item.Ten)
            .ToList();

        DanhSachPoi = new ObservableCollection<PoiListItemViewModel>(items);
    }

    private PoiListItemViewModel TaoItemHienThi(PoiModel poi)
    {
        double? khoangCach = _viTriHienTai == null
            ? null
            : GeofenceService.TinhKhoangCachMetres(_viTriHienTai.Lat, _viTriHienTai.Lng, poi.Lat, poi.Lng);

        var urlAnh = ApiEndpointResolver.BuildPoiImageUrl(poi.TenFileAnhMinhHoa);
        var (trangThai, chiTiet, mau, mauNen, mauVien) = TaoThongTinKhoangCach(khoangCach);

        return new PoiListItemViewModel
        {
            Id = poi.Id,
            Ten = poi.Ten,
            MoTaNgan = RutGonMoTa(ChonMoTaTheoNgonNgu(poi)),
            UrlAnh = urlAnh,
            CoAnh = !string.IsNullOrWhiteSpace(urlAnh),
            ChuCaiDaiDien = TaoChuCaiDaiDien(poi.Ten),
            TrangThaiKhoangCach = trangThai,
            KhoangCachChiTiet = chiTiet,
            MauTrangThai = mau,
            MauNenTrangThai = mauNen,
            MauVienTrangThai = mauVien,
            NgheNgayCommand = NgheNgayCommand,
            XemBanDoCommand = XemBanDoCommand
        };
    }

    private double TinhKhoangCachSapXep(int poiId)
    {
        if (_viTriHienTai == null)
            return double.MaxValue;

        var poi = _poiGoc.FirstOrDefault(x => x.Id == poiId);
        return poi == null
            ? double.MaxValue
            : GeofenceService.TinhKhoangCachMetres(_viTriHienTai.Lat, _viTriHienTai.Lng, poi.Lat, poi.Lng);
    }

    private static int DoUuTienKhoangCach(string trangThaiKhoangCach) => trangThaiKhoangCach switch
    {
        "Rất gần" => 0,
        "Gần" => 1,
        "Xa" => 2,
        _ => 3
    };

    private static (string TrangThai, string ChiTiet, string Mau, string MauNen, string MauVien) TaoThongTinKhoangCach(double? khoangCach)
    {
        if (!khoangCach.HasValue)
        {
            return ("Gần bạn", "Bật GPS để xem khoảng cách", "#D1D5DB", "#1F2937", "#374151");
        }

        var khoangCachLamTron = Math.Round(khoangCach.Value);
        if (khoangCachLamTron < 20)
        {
            return ("Rất gần", $"({khoangCachLamTron:0}m)", "#7CFC8A", "#11261A", "#2C6E49");
        }

        if (khoangCachLamTron < 100)
        {
            return ("Gần", $"({khoangCachLamTron:0}m)", "#FBBF24", "#2B2210", "#8A6D1F");
        }

        return ("Xa", $"({khoangCachLamTron:0}m)", "#60A5FA", "#101B2B", "#275D9A");
    }

    private static string TaoChuCaiDaiDien(string ten)
    {
        if (string.IsNullOrWhiteSpace(ten))
            return "POI";

        var tu = ten.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tu.Length >= 2)
            return $"{char.ToUpperInvariant(tu[0][0])}{char.ToUpperInvariant(tu[1][0])}";

        return ten.Length >= 2
            ? ten[..2].ToUpperInvariant()
            : ten.ToUpperInvariant();
    }

    private static string RutGonMoTa(string moTa)
    {
        if (string.IsNullOrWhiteSpace(moTa))
            return "Khám phá địa điểm này để nghe thuyết minh và xem thông tin nổi bật.";

        var moTaRutGon = moTa.Replace(Environment.NewLine, " ").Trim();
        return moTaRutGon.Length <= 120
            ? moTaRutGon
            : $"{moTaRutGon[..117].TrimEnd()}...";
    }

    private static string ChonMoTaTheoNgonNgu(PoiModel poi)
    {
        string maNgonNgu = Preferences.Get("app_language", Preferences.Get("tts_language", "vi-VN"));
        return PoiDescriptionResolver.GetBestDescriptionOrDefault(poi, maNgonNgu, EmptyDescriptionMessage);
    }

}
