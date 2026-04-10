using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhApi.Data;
using VinhKhanhApi.Models;

namespace VinhKhanhApi.Controllers
{
    public class CmsController : Controller
    {
        private readonly AppDbContext _db;

        public CmsController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var ds = await _db.POIs
                .OrderBy(p => p.UuTien)
                .ToListAsync();

            return View(ds);
        }

        public IActionResult Create()
        {
            return View(new PoiModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PoiModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _db.POIs.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã thêm POI thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var poi = await _db.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            return View(poi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PoiModel model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật POI thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var poi = await _db.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            return View(poi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var poi = await _db.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            _db.POIs.Remove(poi);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xóa POI thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}