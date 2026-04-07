namespace App.Models
{
    // Model đọc kết quả tổng hợp analytics từ bảng LichSuPhat
    public class ThongKePoiNgheModel
    {
        public int PoiId { get; set; }
        public string TenPoi { get; set; } = string.Empty;
        public int SoLanNghe { get; set; }
        public DateTime LanNgheGanNhat { get; set; }
    }
}
