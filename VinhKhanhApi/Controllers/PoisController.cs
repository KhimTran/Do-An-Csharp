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
        public async Task<ActionResult<List<PoiModel>>> GetAll([FromQuery] bool includePending = false)
        {
            var query = _db.POIs.AsQueryable();

            if (!includePending)
                query = query.Where(p => p.TrangThaiDuyet == "Approved");

            var danhSach = await query.OrderBy(p => p.UuTien).ToListAsync();
            return Ok(danhSach);
        }

        // GET api/pois/1 — Lấy 1 POI theo Id
        [HttpGet("{id}")]
        public async Task<ActionResult<PoiModel>> GetById(int id)
        {
            var poi = await _db.POIs.FindAsync(id);
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
            poi.TrangThaiDuyet = "Pending";
            poi.NgayDeXuat = DateTime.UtcNow;
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

            if (request.Approved)
            {
                if (!string.IsNullOrWhiteSpace(poi.NoiDungDeXuat))
                    poi.MoTa_Vi = poi.NoiDungDeXuat;

                poi.TrangThaiDuyet = "Approved";
                poi.LyDoTuChoi = null;
                poi.NoiDungDeXuat = null;
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
        }

        public class ApproveRequest
        {
            public bool Approved { get; set; }
            public string? LyDoTuChoi { get; set; }
            public string? AdminName { get; set; }
        }
    }
}
