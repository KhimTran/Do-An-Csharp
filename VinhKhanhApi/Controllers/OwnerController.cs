using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhApi.Data;
using VinhKhanhApi.ViewModels;

namespace VinhKhanhApi.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        private readonly AppDbContext _db;

        public OwnerController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var poiId = LayPoiIdTuClaims();
            if (!poiId.HasValue) return Forbid();

            var poi = await _db.POIs.FindAsync(poiId.Value);
            if (poi == null) return NotFound();

            var logs = await _db.PlaybackLogs.Where(x => x.PoiId == poi.Id).ToListAsync();

            var vm = new OwnerShopViewModel
            {
                PoiId = poi.Id,
                Ten = poi.Ten,
                SoDienThoai = poi.SoDienThoai,
                GioMoCua = poi.GioMoCua,
                GioDongCua = poi.GioDongCua,
                MonDacTrung = poi.MonDacTrung,
                GalleryJson = poi.GalleryJson,
                NoiDungDeXuat = poi.NoiDungDeXuat,
                TrangThaiDuyet = poi.TrangThaiDuyet,
                LyDoTuChoi = poi.LyDoTuChoi,
                TongLuotNghe = logs.Count,
                LuotQr = logs.Count(x => x.Nguon == "QR"),
                LuotGps = logs.Count(x => x.Nguon == "GPS")
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dashboard(OwnerShopViewModel model)
        {
            var poiId = LayPoiIdTuClaims();
            if (!poiId.HasValue || poiId.Value != model.PoiId) return Forbid();

            var poi = await _db.POIs.FindAsync(model.PoiId);
            if (poi == null) return NotFound();

            // Chủ quán được tự cập nhật info cơ bản trực tiếp.
            poi.SoDienThoai = model.SoDienThoai;
            poi.GioMoCua = model.GioMoCua;
            poi.GioDongCua = model.GioDongCua;
            poi.MonDacTrung = model.MonDacTrung;
            poi.GalleryJson = model.GalleryJson;

            // Nội dung thuyết minh phải qua duyệt admin.
            poi.NoiDungDeXuat = model.NoiDungDeXuat;
            poi.TrangThaiDuyet = "Pending";
            poi.NgayDeXuat = DateTime.UtcNow;
            poi.LyDoTuChoi = null;
            poi.NguoiCapNhat = User.Identity?.Name;

            await _db.SaveChangesAsync();

            TempData["ok"] = "Đã cập nhật thông tin và gửi đề xuất chờ Admin duyệt.";
            return RedirectToAction(nameof(Dashboard));
        }

        private int? LayPoiIdTuClaims()
        {
            var value = User.FindFirstValue("poi_id");
            if (int.TryParse(value, out var poiId)) return poiId;
            return null;
        }
    }
}
