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

        // GET api/pois — App MAUI gọi để lấy danh sách POI
        [HttpGet]
        public async Task<ActionResult<List<PoiModel>>> GetAll()
        {
            var danhSach = await _db.POIs
                .OrderBy(p => p.UuTien)
                .ToListAsync();
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
    }
}