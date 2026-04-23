namespace VinhKhanhApi.ViewModels
{
    public class MonitoringViewModel
    {
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public int ActiveDeviceCount { get; set; }
        public IReadOnlyList<MonitoringActiveDeviceViewModel> ActiveDevices { get; set; } = [];
        public IReadOnlyList<MonitoringWeeklyUsageViewModel> WeeklyUsage { get; set; } = [];
        public IReadOnlyList<MonitoringHeatmapCellViewModel> WeeklyHeatmap { get; set; } = [];
    }

    public class MonitoringActiveDeviceViewModel
    {
        public string SessionId { get; set; } = string.Empty;
        public string DeviceLabel { get; set; } = "Unknown";
        public DateTime LastSeen { get; set; }
        public string? AppVersion { get; set; }
    }

    public class MonitoringWeeklyUsageViewModel
    {
        public int DayIndex { get; set; }
        public string DayLabel { get; set; } = string.Empty;
        public int DistinctSessions { get; set; }
    }

    public class MonitoringHeatmapCellViewModel
    {
        public int DayIndex { get; set; }
        public string DayLabel { get; set; } = string.Empty;
        public int Hour { get; set; }
        public int Count { get; set; }
    }
}
