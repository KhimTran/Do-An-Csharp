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
        private readonly IWebHostEnvironment _env;

        public OwnerController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var poiId = LayPoiIdTuClaims();
            if (!poiId.HasValue) return Forbid();

            var poi = await _db.POIs.FindAsync(poiId.Value);
            if (poi == null) return NotFound();

            var vm = new OwnerShopViewModel
            {
                PoiId = poi.Id,
                Ten = poi.Ten,
                MoTa_Vi = poi.MoTa_Vi,
                MoTa_En = poi.MoTa_En,
                MoTa_Zh = poi.MoTa_Zh,
                TenFileAudio_Vi = poi.TenFileAudio_Vi,
                TenFileAudio_En = poi.TenFileAudio_En,
                TenFileAudio_Zh = poi.TenFileAudio_Zh
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

            poi.MoTa_Vi = model.MoTa_Vi;
            poi.MoTa_En = model.MoTa_En;
            poi.MoTa_Zh = model.MoTa_Zh;
            poi.TenFileAudio_Vi = await LuuFileAudioNeuCo(model.AudioVi, poi.TenFileAudio_Vi);
            poi.TenFileAudio_En = await LuuFileAudioNeuCo(model.AudioEn, poi.TenFileAudio_En);
            poi.TenFileAudio_Zh = await LuuFileAudioNeuCo(model.AudioZh, poi.TenFileAudio_Zh);
            poi.NguoiCapNhat = User.Identity?.Name;

            await _db.SaveChangesAsync();

            TempData["ok"] = "Đã lưu mô tả và audio cho quán.";
            return RedirectToAction(nameof(Dashboard));
        }

        private async Task<string?> LuuFileAudioNeuCo(IFormFile? file, string? fileNameCu)
        {
            if (file == null || file.Length == 0) return fileNameCu;

            var thuMucAudio = Path.Combine(_env.WebRootPath, "audio");
            Directory.CreateDirectory(thuMucAudio);

            var tenMoi = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
            var duongDan = Path.Combine(thuMucAudio, tenMoi);

            await using var stream = System.IO.File.Create(duongDan);
            await file.CopyToAsync(stream);
            return tenMoi;
        }

        private int? LayPoiIdTuClaims()
        {
            var value = User.FindFirstValue("poi_id");
            if (int.TryParse(value, out var poiId)) return poiId;
            return null;
        }
    }
}
