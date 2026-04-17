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

        // Global analytics cho Admin: heatmap tuyến phố.
        [HttpGet("heatmap")]
        public async Task<IActionResult> Heatmap()
        {
            var points = await _db.PlaybackLogs
                .Join(_db.POIs, l => l.PoiId, p => p.Id, (l, p) => new { p.Lat, p.Lng })
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

        // Local analytics cho chủ quán.
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
    }
}
