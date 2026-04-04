using SQLite;

namespace App.Models
{
    [Table("LichSuPhat")]
    public class LichSuPhatModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int PoiId { get; set; }           // FK liên kết POIs
        public string TenPoi { get; set; } = string.Empty;  // Lưu để dễ debug
        public string NgonNgu { get; set; } = "vi"; // 'vi', 'en', 'zh'
        public DateTime ThoiGianPhat { get; set; } = DateTime.Now;
        public string NguonKichHoat { get; set; } = "GPS"; // 'GPS' hoặc 'QR'
    }
}