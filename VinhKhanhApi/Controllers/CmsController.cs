using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
                TenFileAnhMinhHoa = poi.TenFileAnhMinhHoa,
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

            poi.NguoiCapNhat = "admin";

            poi.TenFileAudio_Vi = await LuuFileAudioNeuCo(model.AudioVi, model.TenFileAudio_Vi);
            poi.TenFileAudio_En = await LuuFileAudioNeuCo(model.AudioEn, model.TenFileAudio_En);
            poi.TenFileAudio_Zh = await LuuFileAudioNeuCo(model.AudioZh, model.TenFileAudio_Zh);
            poi.TenFileAnhMinhHoa = await LuuFileAnhNeuCo(model.AnhMinhHoa, model.TenFileAnhMinhHoa);

            await _db.SaveChangesAsync();
            await DongBoQrCodeTheoIdAsync(isNewPoi ? poi.Id : null);
            return RedirectToAction(nameof(Index));
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
                await DongBoQrCodeTheoIdAsync();
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdminAccount(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return RedirectToAction(nameof(Users));

            if (await _db.UserAccounts.AnyAsync(x => x.Username == username))
            {
                TempData["err"] = "Username đã tồn tại";
                return RedirectToAction(nameof(Users));
            }

            _db.UserAccounts.Add(new UserAccountModel
            {
                Username = username.Trim(),
                PasswordHash = Services.PasswordHasher.Hash(password),
                Role = "Admin",
                PoiId = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            TempData["ok"] = "Đã tạo tài khoản quản trị viên";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserLock(int id)
        {
            var user = await _db.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == user.Id.ToString())
            {
                TempData["err"] = "Không thể tự khóa tài khoản đang đăng nhập";
                return RedirectToAction(nameof(Users));
            }

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();

            TempData["ok"] = user.IsActive ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _db.UserAccounts.FindAsync(id);
            if (user == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == user.Id.ToString())
            {
                TempData["err"] = "Không thể xóa tài khoản đang đăng nhập";
                return RedirectToAction(nameof(Users));
            }

            _db.UserAccounts.Remove(user);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Đã xóa tài khoản";
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

            var lichSuSuDung = logs
                .OrderByDescending(x => x.ThoiGianNghe)
                .Take(100)
                .Select(x => new
                {
                    x.PoiTen,
                    x.Nguon,
                    x.ThoiLuongGiay,
                    x.ThoiGianNghe
                })
                .ToList();

            var heatmap = await _db.RoutePings
                .GroupBy(x => new
                {
                    Lat = Math.Round(x.Lat, 4),
                    Lng = Math.Round(x.Lng, 4)
                })
                .Select(g => new
                {
                    g.Key.Lat,
                    g.Key.Lng,
                    SoLuot = g.Count()
                })
                .OrderByDescending(x => x.SoLuot)
                .Take(100)
                .ToListAsync();

            ViewData["TongLuotNghe"] = logs.Count;
            ViewData["ThoiLuongTrungBinh"] = logs.Count == 0 ? 0 : logs.Average(x => x.ThoiLuongGiay);
            ViewData["LichSuSuDung"] = lichSuSuDung;
            ViewData["Heatmap"] = heatmap;
            return View(topPoi);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Monitoring()
        {
            var now = DateTime.UtcNow;
            var activeCutoff = now.AddSeconds(-90);
            var weeklyCutoff = now.Date.AddDays(-6);

            var activeDevices = await _db.DeviceHeartbeats
                .AsNoTracking()
                .Where(x => x.LastSeen >= activeCutoff)
                .OrderByDescending(x => x.LastSeen)
                .Select(x => new
                {
                    x.SessionId,
                    x.DeviceLabel,
                    x.LastSeen,
                    x.AppVersion
                })
                .ToListAsync();

            var recentRoutePings = await _db.RoutePings
                .AsNoTracking()
                .Where(x => x.ThoiGian >= weeklyCutoff)
                .Select(x => new
                {
                    x.SessionId,
                    x.ThoiGian
                })
                .ToListAsync();

            var orderedDays = new[]
            {
                new { Day = DayOfWeek.Monday, Label = "Thứ 2", Index = 0 },
                new { Day = DayOfWeek.Tuesday, Label = "Thứ 3", Index = 1 },
                new { Day = DayOfWeek.Wednesday, Label = "Thứ 4", Index = 2 },
                new { Day = DayOfWeek.Thursday, Label = "Thứ 5", Index = 3 },
                new { Day = DayOfWeek.Friday, Label = "Thứ 6", Index = 4 },
                new { Day = DayOfWeek.Saturday, Label = "Thứ 7", Index = 5 },
                new { Day = DayOfWeek.Sunday, Label = "Chủ nhật", Index = 6 }
            };

            var weeklyUsage = orderedDays
                .Select(day => new
                {
                    day.Index,
                    day.Label,
                    DistinctSessions = recentRoutePings
                        .Where(x => x.ThoiGian.DayOfWeek == day.Day && !string.IsNullOrWhiteSpace(x.SessionId))
                        .Select(x => x.SessionId)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count()
                })
                .ToList();

            var weeklyHeatmap = orderedDays
                .SelectMany(day => Enumerable.Range(0, 24).Select(hour => new
                {
                    day.Index,
                    day.Label,
                    Hour = hour,
                    Count = recentRoutePings.Count(x => x.ThoiGian.DayOfWeek == day.Day && x.ThoiGian.Hour == hour)
                }))
                .ToList();

            var model = new MonitoringViewModel
            {
                GeneratedAtUtc = now,
                ActiveDeviceCount = activeDevices.Count,
                ActiveDevices = activeDevices
                    .Select(x => new MonitoringActiveDeviceViewModel
                    {
                        SessionId = x.SessionId,
                        DeviceLabel = x.DeviceLabel,
                        LastSeen = x.LastSeen,
                        AppVersion = x.AppVersion
                    })
                    .ToList(),
                WeeklyUsage = weeklyUsage
                    .Select(x => new MonitoringWeeklyUsageViewModel
                    {
                        DayIndex = x.Index,
                        DayLabel = x.Label,
                        DistinctSessions = x.DistinctSessions
                    })
                    .ToList(),
                WeeklyHeatmap = weeklyHeatmap
                    .Select(x => new MonitoringHeatmapCellViewModel
                    {
                        DayIndex = x.Index,
                        DayLabel = x.Label,
                        Hour = x.Hour,
                        Count = x.Count
                    })
                    .ToList()
            };

            return View(model);
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

        private async Task<string?> LuuFileAnhNeuCo(IFormFile? file, string? fileNameCu)
        {
            if (file == null || file.Length == 0) return fileNameCu;

            var thuMucAnh = Path.Combine(_env.WebRootPath, "images", "poi");
            Directory.CreateDirectory(thuMucAnh);

            var tenMoi = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
            var duongDan = Path.Combine(thuMucAnh, tenMoi);

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

        // Backward-compatible aliases to avoid CS0103 if older call sites still exist.
        private Task DongBoOrCodeTheoIdAsync(int? poiId = null) => DongBoQrCodeTheoIdAsync(poiId);
        private Task ResequencePoiIdsAsync() => DongBoQrCodeTheoIdAsync();
    }
}
