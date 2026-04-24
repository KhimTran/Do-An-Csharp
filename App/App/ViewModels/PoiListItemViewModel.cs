using System.Windows.Input;

namespace App.ViewModels;

public sealed class PoiListItemViewModel
{
    public int Id { get; init; }
    public string Ten { get; init; } = string.Empty;
    public string MoTaNgan { get; init; } = string.Empty;
    public string? UrlAnh { get; init; }
    public bool CoAnh { get; init; }
    public bool KhongCoAnh => !CoAnh;
    public string ChuCaiDaiDien { get; init; } = "POI";
    public string TrangThaiKhoangCach { get; init; } = "Gần bạn";
    public string KhoangCachChiTiet { get; init; } = "Bật GPS để xem khoảng cách";
    public string MauTrangThai { get; init; } = "#D1D5DB";
    public string MauNenTrangThai { get; init; } = "#1F2937";
    public string MauVienTrangThai { get; init; } = "#374151";
    public ICommand? NgheNgayCommand { get; init; }
    public ICommand? XemBanDoCommand { get; init; }
}
