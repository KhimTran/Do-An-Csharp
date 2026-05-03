using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhApi.Data;
using VinhKhanhApi.Models;

namespace VinhKhanhApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PoisController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PoisController(AppDbContext db) => _db = db;

        // GET api/pois — App MAUI gọi để lấy danh sách POI đã duyệt.
        [HttpGet]
        public async Task<ActionResult<List<PoiApiItemDto>>> GetAll()
        {
            ApplyNoCacheHeaders();

            var query = _db.POIs
                .AsNoTracking()
                .Where(p => p.TrangThaiDuyet == "Approved")
                .AsQueryable();

            var danhSach = await query
                .OrderBy(p => p.UuTien)
                .Select(MapToDtoExpression())
                .ToListAsync();
            return Ok(danhSach);
        }

        // GET api/pois/1 — Lấy 1 POI theo Id
        [HttpGet("{id}")]
        public async Task<ActionResult<PoiApiItemDto>> GetById(int id)
        {
            ApplyNoCacheHeaders();

            var poi = await _db.POIs
                .AsNoTracking()
                .Where(p => p.Id == id && p.TrangThaiDuyet == "Approved")
                .Select(MapToDtoExpression())
                .FirstOrDefaultAsync();
            if (poi == null) return NotFound();
            return Ok(poi);
        }

        // POST api/pois — CMS web tạo POI mới
        [HttpPost]
        public async Task<ActionResult<PoiModel>> Create(PoiModel poi)
        {
            _db.POIs.Add(poi);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = poi.Id }, poi);
        }

        // PUT api/pois/1 — CMS web cập nhật POI
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PoiModel poi)
        {
            if (id != poi.Id) return BadRequest();
            _db.Entry(poi).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/pois/1 — CMS web xóa POI
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var poi = await _db.POIs.FindAsync(id);
            if (poi == null) return NotFound();
            _db.POIs.Remove(poi);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST api/pois/{id}/owner-submit
        // Chủ quán submit thông tin & text để admin duyệt.
        [HttpPost("{id}/owner-submit")]
        public async Task<IActionResult> OwnerSubmit(int id, [FromBody] OwnerSubmitRequest request)
        {
            var poi = await _db.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            poi.SoDienThoai = request.SoDienThoai;
            poi.GioMoCua = request.GioMoCua;
            poi.GioDongCua = request.GioDongCua;
            poi.MonDacTrung = request.MonDacTrung;
            poi.GalleryJson = request.GalleryJson;
            poi.NoiDungDeXuat = request.NoiDungDeXuat;
            poi.MoTaEnDeXuat = request.MoTaEnDeXuat;
            poi.MoTaZhDeXuat = request.MoTaZhDeXuat;
            poi.AudioFileViDeXuat = request.AudioFileViDeXuat;
            poi.AudioFileEnDeXuat = request.AudioFileEnDeXuat;
            poi.AudioFileZhDeXuat = request.AudioFileZhDeXuat;
            poi.ImagePathDeXuat = request.ImagePathDeXuat;
            poi.TrangThaiDuyet = "Pending";
            poi.NgayDeXuat = DateTime.UtcNow;
            poi.NgayDuyet = null;
            poi.LyDoTuChoi = null;
            poi.NguoiCapNhat = request.TenChuQuan;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã gửi yêu cầu duyệt" });
        }

        // POST api/pois/{id}/approve
        // Admin duyệt hoặc từ chối nội dung do chủ quán submit.
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRequest request)
        {
            var poi = await _db.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            if (!string.Equals(poi.TrangThaiDuyet, "Pending", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Chỉ có đề xuất Pending mới được duyệt hoặc từ chối." });

            if (request.Approved)
            {
                if (!string.IsNullOrWhiteSpace(poi.NoiDungDeXuat))
                    poi.MoTa_Vi = poi.NoiDungDeXuat.Trim();

                if (!string.IsNullOrWhiteSpace(poi.MoTaEnDeXuat))
                    poi.MoTa_En = poi.MoTaEnDeXuat.Trim();

                if (!string.IsNullOrWhiteSpace(poi.MoTaZhDeXuat))
                    poi.MoTa_Zh = poi.MoTaZhDeXuat.Trim();

                if (!string.IsNullOrWhiteSpace(poi.AudioFileViDeXuat))
                    poi.TenFileAudio_Vi = poi.AudioFileViDeXuat;

                if (!string.IsNullOrWhiteSpace(poi.AudioFileEnDeXuat))
                    poi.TenFileAudio_En = poi.AudioFileEnDeXuat;

                if (!string.IsNullOrWhiteSpace(poi.AudioFileZhDeXuat))
                    poi.TenFileAudio_Zh = poi.AudioFileZhDeXuat;

                if (!string.IsNullOrWhiteSpace(poi.ImagePathDeXuat))
                    poi.TenFileAnhMinhHoa = poi.ImagePathDeXuat;

                poi.TrangThaiDuyet = "Approved";
                poi.LyDoTuChoi = null;
                poi.NoiDungDeXuat = null;
                poi.MoTaEnDeXuat = null;
                poi.MoTaZhDeXuat = null;
                poi.AudioFileViDeXuat = null;
                poi.AudioFileEnDeXuat = null;
                poi.AudioFileZhDeXuat = null;
                poi.ImagePathDeXuat = null;
            }
            else
            {
                poi.TrangThaiDuyet = "Rejected";
                poi.LyDoTuChoi = request.LyDoTuChoi;
            }

            poi.NgayDuyet = DateTime.UtcNow;
            poi.NguoiCapNhat = request.AdminName;
            await _db.SaveChangesAsync();

            return Ok(new { poi.Id, poi.TrangThaiDuyet, poi.NgayDuyet });
        }

        public class OwnerSubmitRequest
        {
            public string? TenChuQuan { get; set; }
            public string? SoDienThoai { get; set; }
            public string? GioMoCua { get; set; }
            public string? GioDongCua { get; set; }
            public string? MonDacTrung { get; set; }
            public string? GalleryJson { get; set; }
            public string? NoiDungDeXuat { get; set; }
            public string? MoTaEnDeXuat { get; set; }
            public string? MoTaZhDeXuat { get; set; }
            public string? AudioFileViDeXuat { get; set; }
            public string? AudioFileEnDeXuat { get; set; }
            public string? AudioFileZhDeXuat { get; set; }
            public string? ImagePathDeXuat { get; set; }
        }

        public class ApproveRequest
        {
            public bool Approved { get; set; }
            public string? LyDoTuChoi { get; set; }
            public string? AdminName { get; set; }
        }

        public class PoiApiItemDto
        {
            public int Id { get; set; }
            public string Ten { get; set; } = string.Empty;
            public string? MoTa_Vi { get; set; }
            public string? MoTa_En { get; set; }
            public string? MoTa_Zh { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
            public double BanKinh { get; set; }
            public int UuTien { get; set; }
            public string? TenFileAudio_Vi { get; set; }
            public string? TenFileAudio_En { get; set; }
            public string? TenFileAudio_Zh { get; set; }
            public string? TenFileAnhMinhHoa { get; set; }
            public string? SoDienThoai { get; set; }
            public string? GioMoCua { get; set; }
            public string? GioDongCua { get; set; }
            public string? MonDacTrung { get; set; }
            public string? GalleryJson { get; set; }
            public string? TrangThaiDuyet { get; set; }
            public string? QrCodeNoiDung { get; set; }
        }

        private static System.Linq.Expressions.Expression<Func<PoiModel, PoiApiItemDto>> MapToDtoExpression() =>
            poi => new PoiApiItemDto
            {
                Id = poi.Id,
                Ten = poi.Ten,
                MoTa_Vi = poi.MoTa_Vi,
                MoTa_En = poi.MoTa_En,
                MoTa_Zh = poi.MoTa_Zh,
                Lat = poi.Lat,
                Lng = poi.Lng,
                BanKinh = poi.BanKinh,
                UuTien = poi.UuTien,
                TenFileAudio_Vi = poi.TenFileAudio_Vi,
                TenFileAudio_En = poi.TenFileAudio_En,
                TenFileAudio_Zh = poi.TenFileAudio_Zh,
                TenFileAnhMinhHoa = poi.TenFileAnhMinhHoa,
                SoDienThoai = poi.SoDienThoai,
                GioMoCua = poi.GioMoCua,
                GioDongCua = poi.GioDongCua,
                MonDacTrung = poi.MonDacTrung,
                GalleryJson = poi.GalleryJson,
                TrangThaiDuyet = poi.TrangThaiDuyet,
                QrCodeNoiDung = poi.QrCodeNoiDung
            };

        private void ApplyNoCacheHeaders()
        {
            Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
        }
    }
}
