namespace VinhKhanhApi.Models
{
    public class PlaybackLogModel
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public string PoiTen { get; set; } = string.Empty;
        public string Nguon { get; set; } = "APP";
        public int ThoiLuongGiay { get; set; }
        public DateTime ThoiGianNghe { get; set; } = DateTime.UtcNow;
    }
}
