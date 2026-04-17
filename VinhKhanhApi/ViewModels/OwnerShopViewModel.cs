namespace VinhKhanhApi.ViewModels
{
    public class OwnerShopViewModel
    {
        public int PoiId { get; set; }
        public string Ten { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? GioMoCua { get; set; }
        public string? GioDongCua { get; set; }
        public string? MonDacTrung { get; set; }
        public string? GalleryJson { get; set; }
        public string? NoiDungDeXuat { get; set; }
        public string TrangThaiDuyet { get; set; } = "Approved";
        public string? LyDoTuChoi { get; set; }
        public int TongLuotNghe { get; set; }
        public int LuotQr { get; set; }
        public int LuotGps { get; set; }
    }
}
