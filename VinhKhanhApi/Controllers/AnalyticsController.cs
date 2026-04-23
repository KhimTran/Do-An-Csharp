using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhApi.Data;
using VinhKhanhApi.Models;

namespace VinhKhanhApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AnalyticsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("logs")]
        public async Task<IActionResult> CreateLog([FromBody] PlaybackLogModel log)
        {
            if (string.IsNullOrWhiteSpace(log.PoiTen))
            {
                var poi = await _db.POIs.FindAsync(log.PoiId);
                log.PoiTen = poi?.Ten ?? $"POI {log.PoiId}";
            }

            log.ThoiGianNghe = DateTime.UtcNow;
            _db.PlaybackLogs.Add(log);
            await _db.SaveChangesAsync();
            return Ok(log);
        }

        [HttpGet("top-pois")]
        public async Task<IActionResult> TopPois()
        {
            var data = await _db.PlaybackLogs
                .GroupBy(x => x.PoiTen)
                .Select(g => new { TenPoi = g.Key, SoLan = g.Count() })
                .OrderByDescending(x => x.SoLan)
                .Take(10)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var total = await _db.PlaybackLogs.CountAsync();
            var avgDuration = total == 0
                ? 0
                : await _db.PlaybackLogs.AverageAsync(x => x.ThoiLuongGiay);

            return Ok(new
            {
                TongLuotNghe = total,
                ThoiLuongTrungBinh = avgDuration
            });
        }

        [HttpGet("heatmap")]
        public async Task<IActionResult> Heatmap()
        {
            var points = await _db.RoutePings
                .GroupBy(x => new
                {
                    Lat = Math.Round(x.Lat, 4),
                    Lng = Math.Round(x.Lng, 4)
                })
                .Select(g => new
                {
                    g.Key.Lat,
                    g.Key.Lng,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return Ok(points);
        }

        [HttpPost("route-pings")]
        public async Task<IActionResult> CreateRoutePings([FromBody] List<RoutePingModel>? pings)
        {
            if (pings == null || pings.Count == 0)
                return BadRequest("Danh sach route ping rong.");

            foreach (var ping in pings.Where(p => p is not null))
            {
                ping.SessionId = string.IsNullOrWhiteSpace(ping.SessionId) ? "unknown" : ping.SessionId.Trim();
                ping.ThoiGian = ping.ThoiGian == default ? DateTime.UtcNow : ping.ThoiGian;
                ping.Nguon = string.IsNullOrWhiteSpace(ping.Nguon) ? "GPS" : ping.Nguon.Trim().ToUpperInvariant();
            }

            _db.RoutePings.AddRange(pings);
            await _db.SaveChangesAsync();
            return Ok(new { Saved = pings.Count });
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] DeviceHeartbeatRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
                return BadRequest("sessionId is required.");

            var sessionId = request.SessionId.Trim();
            var deviceLabel = string.IsNullOrWhiteSpace(request.DeviceLabel)
                ? "Unknown"
                : request.DeviceLabel.Trim();
            var appVersion = string.IsNullOrWhiteSpace(request.AppVersion)
                ? null
                : request.AppVersion.Trim();
            var lastSeen = DateTime.UtcNow;

            await _db.Database.ExecuteSqlInterpolatedAsync($@"
IF EXISTS (SELECT 1 FROM DeviceHeartbeats WITH (UPDLOCK, SERIALIZABLE) WHERE SessionId = {sessionId})
BEGIN
    UPDATE DeviceHeartbeats
    SET DeviceLabel = {deviceLabel},
        LastSeen = {lastSeen},
        AppVersion = {appVersion}
    WHERE SessionId = {sessionId};
END
ELSE
BEGIN
    INSERT INTO DeviceHeartbeats (SessionId, DeviceLabel, LastSeen, AppVersion)
    VALUES ({sessionId}, {deviceLabel}, {lastSeen}, {appVersion});
END");

            return Ok();
        }

        [HttpGet("active-devices")]
        public async Task<IActionResult> ActiveDevices()
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-90);

            var devices = await _db.DeviceHeartbeats
                .AsNoTracking()
                .Where(x => x.LastSeen >= cutoff)
                .OrderByDescending(x => x.LastSeen)
                .Select(x => new
                {
                    x.SessionId,
                    x.DeviceLabel,
                    x.LastSeen,
                    x.AppVersion
                })
                .ToListAsync();

            return Ok(new
            {
                Count = devices.Count,
                Devices = devices
            });
        }

        [HttpGet("local/{poiId:int}")]
        public async Task<IActionResult> LocalAnalytics(int poiId)
        {
            var logs = await _db.PlaybackLogs.Where(x => x.PoiId == poiId).ToListAsync();
            var total = logs.Count;
            var qrScans = logs.Count(x => x.Nguon == "QR");
            var gpsAuto = logs.Count(x => x.Nguon == "GPS");
            var avgDuration = total == 0 ? 0 : logs.Average(x => x.ThoiLuongGiay);

            return Ok(new
            {
                PoiId = poiId,
                TongLuotNghe = total,
                LuotQr = qrScans,
                LuotGps = gpsAuto,
                ThoiLuongTrungBinh = avgDuration
            });
        }

        public sealed class DeviceHeartbeatRequest
        {
            public string SessionId { get; set; } = string.Empty;
            public string? DeviceLabel { get; set; }
            public string? AppVersion { get; set; }
        }
    }
}
