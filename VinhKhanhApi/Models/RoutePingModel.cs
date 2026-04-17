namespace VinhKhanhApi.Models
{
    // Dữ liệu tuyến di chuyển ẩn danh gửi từ app để phục vụ analytics/heatmap.
    public class RoutePingModel
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.UtcNow;
        public string Nguon { get; set; } = "GPS";
    }
}
