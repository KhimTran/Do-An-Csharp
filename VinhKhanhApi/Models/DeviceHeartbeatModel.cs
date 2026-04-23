namespace VinhKhanhApi.Models
{
    public class DeviceHeartbeatModel
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string DeviceLabel { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public string? AppVersion { get; set; }
    }
}
