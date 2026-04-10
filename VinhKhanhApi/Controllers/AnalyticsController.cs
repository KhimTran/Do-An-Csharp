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
    }
}
