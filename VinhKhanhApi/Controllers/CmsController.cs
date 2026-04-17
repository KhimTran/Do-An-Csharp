using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using VinhKhanhApi.Data;
using VinhKhanhApi.Models;
using VinhKhanhApi.ViewModels;

namespace VinhKhanhApi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CmsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CmsController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _db.POIs.OrderBy(p => p.UuTien).ToListAsync();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return View(new CmsPoiFormViewModel());

            var poi = await _db.POIs.FindAsync(id.Value);
            if (poi == null) return NotFound();

            return View(new CmsPoiFormViewModel
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
                SoDienThoai = poi.SoDienThoai,
                GioMoCua = poi.GioMoCua,
                GioDongCua = poi.GioDongCua,
                MonDacTrung = poi.MonDacTrung,
                GalleryJson = poi.GalleryJson,
                QrCodeNoiDung = poi.QrCodeNoiDung,
                TtsVoiceCode = poi.TtsVoiceCode,
                TrangThaiDuyet = poi.TrangThaiDuyet,
                NoiDungDeXuat = poi.NoiDungDeXuat,
                NgayDeXuat = poi.NgayDeXuat,
                NgayDuyet = poi.NgayDuyet,
                LyDoTuChoi = poi.LyDoTuChoi
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CmsPoiFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            PoiModel poi;
            var isNewPoi = model.Id == 0;
            if (model.Id == 0)
            {
                poi = new PoiModel();
                _db.POIs.Add(poi);
            }
            else
            {
                poi = await _db.POIs.FindAsync(model.Id) ?? new PoiModel();
                if (poi.Id == 0) _db.POIs.Add(poi);
            }

            poi.Ten = model.Ten;
            poi.MoTa_Vi = model.MoTa_Vi;
            poi.MoTa_En = model.MoTa_En;
            poi.MoTa_Zh = model.MoTa_Zh;
            poi.Lat = model.Lat;
            poi.Lng = model.Lng;
            poi.BanKinh = model.BanKinh;
            poi.UuTien = model.UuTien;

            poi.SoDienThoai = model.SoDienThoai;
            poi.GioMoCua = model.GioMoCua;
            poi.GioDongCua = model.GioDongCua;
            poi.MonDacTrung = model.MonDacTrung;
            poi.GalleryJson = model.GalleryJson;
            poi.QrCodeNoiDung = model.QrCodeNoiDung;
            poi.TtsVoiceCode = string.IsNullOrWhiteSpace(model.TtsVoiceCode) ? "vi-VN" : model.TtsVoiceCode;
            poi.NguoiCapNhat = "admin";

            poi.TenFileAudio_Vi = await LuuFileAudioNeuCo(model.AudioVi, model.TenFileAudio_Vi);
            poi.TenFileAudio_En = await LuuFileAudioNeuCo(model.AudioEn, model.TenFileAudio_En);
            poi.TenFileAudio_Zh = await LuuFileAudioNeuCo(model.AudioZh, model.TenFileAudio_Zh);

            await _db.SaveChangesAsync();
            await DongBoQrCodeTheoIdAsync(isNewPoi ? poi.Id : null);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, bool approve, string? lyDoTuChoi)
        {
            var poi = await _db.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            if (approve)
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
                poi.LyDoTuChoi = lyDoTuChoi;
            }

            poi.NgayDuyet = DateTime.UtcNow;
            poi.NguoiCapNhat = "admin";
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var poi = await _db.POIs.FindAsync(id);
            if (poi != null)
            {
                _db.POIs.Remove(poi);
                await _db.SaveChangesAsync();
                await ResequencePoiIdsAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _db.UserAccounts.OrderBy(x => x.Role).ThenBy(x => x.Username).ToListAsync();
            ViewData["Pois"] = await _db.POIs.OrderBy(x => x.Ten).ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOwnerAccount(string username, string password, int poiId)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return RedirectToAction(nameof(Users));

            if (await _db.UserAccounts.AnyAsync(x => x.Username == username))
            {
                TempData["err"] = "Username đã tồn tại";
                return RedirectToAction(nameof(Users));
            }

            _db.UserAccounts.Add(new Models.UserAccountModel
            {
                Username = username.Trim(),
                PasswordHash = Services.PasswordHasher.Hash(password),
                Role = "Owner",
                PoiId = poiId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã tạo tài khoản chủ quán";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var logs = await _db.PlaybackLogs.ToListAsync();
            var topPoi = logs.GroupBy(x => x.PoiTen)
                .Select(g => new { TenPoi = g.Key, SoLan = g.Count() })
                .OrderByDescending(x => x.SoLan)
                .Take(10)
                .ToList();

            ViewData["TongLuotNghe"] = logs.Count;
            ViewData["ThoiLuongTrungBinh"] = logs.Count == 0 ? 0 : logs.Average(x => x.ThoiLuongGiay);
            return View(topPoi);
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

        private async Task DongBoQrCodeTheoIdAsync(int? poiId = null)
        {
            if (poiId.HasValue)
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [POIs] SET [QrCodeNoiDung] = CONCAT('poi:', [Id]) WHERE [Id] = {poiId.Value} AND ([QrCodeNoiDung] IS NULL OR [QrCodeNoiDung] = '' OR [QrCodeNoiDung] LIKE 'poi:%')");
                return;
            }

            await _db.Database.ExecuteSqlRawAsync(
                "UPDATE [POIs] SET [QrCodeNoiDung] = CONCAT('poi:', [Id]) WHERE [QrCodeNoiDung] IS NULL OR [QrCodeNoiDung] = '' OR [QrCodeNoiDung] LIKE 'poi:%'");
        }
    }
}
