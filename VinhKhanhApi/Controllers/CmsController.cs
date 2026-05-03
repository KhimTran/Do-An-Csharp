using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VinhKhanhApi.Data;
using VinhKhanhApi.Models;
using VinhKhanhApi.Services;
using VinhKhanhApi.ViewModels;

namespace VinhKhanhApi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CmsController : Controller
    {
        private const long KichThuocAudioToiDa = 20 * 1024 * 1024;

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ITranslationService _translationService;
        private readonly IQrCodeService _qrCodeService;
        private readonly IAudioFileCleanupService _audioFileCleanupService;

        public CmsController(
            AppDbContext db,
            IWebHostEnvironment env,
            ITranslationService translationService,
            IQrCodeService qrCodeService,
            IAudioFileCleanupService audioFileCleanupService)
        {
            _db = db;
            _env = env;
            _translationService = translationService;
            _qrCodeService = qrCodeService;
            _audioFileCleanupService = audioFileCleanupService;
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
            if (id == null)
            {
                return View(new CmsPoiFormViewModel
                {
                    SourceLanguage = CmsPoiFormViewModel.VietnameseLanguage,
                    ActiveDescriptionTab = CmsPoiFormViewModel.VietnameseLanguage
                });
            }

            var poi = await _db.POIs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id.Value);
            if (poi == null) return NotFound();

            return View(MapToPoiFormViewModel(poi));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CmsPoiFormViewModel model, CancellationToken cancellationToken)
        {
            model.SourceLanguage = CmsPoiFormViewModel.NormalizeLanguage(model.SourceLanguage);
            model.ActiveDescriptionTab = model.SourceLanguage;

            PoiModel? poi = null;
            if (model.Id != 0)
            {
                poi = await _db.POIs.FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
                if (poi == null) return NotFound();
            }

            if (!ModelState.IsValid)
            {
                RestoreCurrentFiles(model, poi);
                return View(model);
            }

            if (!await TryPopulateTranslatedDescriptionsAsync(model, poi, cancellationToken))
            {
                RestoreCurrentFiles(model, poi);
                return View(model);
            }

            var isNewPoi = poi == null;
            if (isNewPoi)
            {
                poi = new PoiModel();
                _db.POIs.Add(poi);
            }

            var targetPoi = poi!;
            var audioCuCanXoa = new List<string?>();
            var tenFileAudioViCu = targetPoi.TenFileAudio_Vi;
            var tenFileAudioEnCu = targetPoi.TenFileAudio_En;
            var tenFileAudioZhCu = targetPoi.TenFileAudio_Zh;

            ApplyFormToPoi(targetPoi, model);
            targetPoi.NguoiCapNhat = User.Identity?.Name ?? "admin";

            var tenFileAnhMinhHoa = await LuuFileAnhNeuCo(model.AnhMinhHoa, targetPoi.TenFileAnhMinhHoa);
            var tenFileAudioVi = await CapNhatFileAudioAsync(
                model.AudioVi,
                targetPoi.TenFileAudio_Vi,
                model.XoaAudioVi,
                nameof(CmsPoiFormViewModel.AudioVi));
            var tenFileAudioEn = await CapNhatFileAudioAsync(
                model.AudioEn,
                targetPoi.TenFileAudio_En,
                model.XoaAudioEn,
                nameof(CmsPoiFormViewModel.AudioEn));
            var tenFileAudioZh = await CapNhatFileAudioAsync(
                model.AudioZh,
                targetPoi.TenFileAudio_Zh,
                model.XoaAudioZh,
                nameof(CmsPoiFormViewModel.AudioZh));

            if (!ModelState.IsValid)
            {
                RestoreCurrentFiles(model, targetPoi);
                return View(model);
            }

            targetPoi.TenFileAnhMinhHoa = tenFileAnhMinhHoa;
            targetPoi.TenFileAudio_Vi = tenFileAudioVi;
            targetPoi.TenFileAudio_En = tenFileAudioEn;
            targetPoi.TenFileAudio_Zh = tenFileAudioZh;

            await _db.SaveChangesAsync(cancellationToken);

            ThemAudioCuCanXoa(audioCuCanXoa, tenFileAudioViCu, tenFileAudioVi);
            ThemAudioCuCanXoa(audioCuCanXoa, tenFileAudioEnCu, tenFileAudioEn);
            ThemAudioCuCanXoa(audioCuCanXoa, tenFileAudioZhCu, tenFileAudioZh);
            await XoaAudioKhongConSuDungAsync(audioCuCanXoa, cancellationToken);

            await DongBoQrCodeTheoIdAsync(isNewPoi ? targetPoi.Id : null);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var poi = await _db.POIs.FindAsync(id);
            if (poi != null)
            {
                var audioCanXoa = LayTatCaAudioCuaPoi(poi);
                _db.POIs.Remove(poi);
                await _db.SaveChangesAsync();
                await XoaAudioKhongConSuDungAsync(audioCanXoa);
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

        [HttpGet]
        public async Task<IActionResult> QrCodes()
        {
            var pois = await _db.POIs
                .OrderBy(x => x.UuTien)
                .ThenBy(x => x.Ten)
                .ToListAsync();

            var changed = false;
            foreach (var poi in pois)
            {
                var expectedQrContent = $"poi:{poi.Id}";
                if (string.IsNullOrWhiteSpace(poi.QrCodeNoiDung))
                {
                    poi.QrCodeNoiDung = expectedQrContent;
                    changed = true;
                }
            }

            if (changed)
            {
                await _db.SaveChangesAsync();
            }

            var items = pois
                .Select(poi =>
                {
                    var qrContent = string.IsNullOrWhiteSpace(poi.QrCodeNoiDung)
                        ? $"poi:{poi.Id}"
                        : poi.QrCodeNoiDung!;

                    return new CmsQrCodeItemViewModel
                    {
                        Id = poi.Id,
                        TenPoi = poi.Ten,
                        ViTriHienThi = $"Lat: {poi.Lat:F6} | Lng: {poi.Lng:F6}",
                        QrContent = qrContent,
                        DaCoQr = !string.IsNullOrWhiteSpace(poi.QrCodeNoiDung),
                        QrImageBase64 = _qrCodeService.GenerateQrPngBase64(qrContent)
                    };
                })
                .ToList();

            var model = new CmsQrCodesViewModel
            {
                TongPoi = items.Count,
                DaCoQr = items.Count(x => x.DaCoQr),
                ChuaCoQr = items.Count(x => !x.DaCoQr),
                Items = items
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> OwnerSubmissions()
        {
            var submissions = await _db.POIs
                .AsNoTracking()
                .Where(x => x.TrangThaiDuyet == "Pending")
                .OrderByDescending(x => x.NgayDeXuat)
                .ThenBy(x => x.Ten)
                .ToListAsync();

            return View(submissions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOwnerSubmission(int id, CancellationToken cancellationToken)
        {
            var poi = await _db.POIs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (poi == null) return NotFound();

            if (!string.Equals(poi.TrangThaiDuyet, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["err"] = "Chỉ có đề xuất Pending mới được duyệt.";
                return RedirectToAction(nameof(OwnerSubmissions));
            }

            var audioCuCanXoa = new List<string?>();
            var tenFileAudioViCu = poi.TenFileAudio_Vi;
            var tenFileAudioEnCu = poi.TenFileAudio_En;
            var tenFileAudioZhCu = poi.TenFileAudio_Zh;

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
            poi.NgayDuyet = DateTime.UtcNow;
            poi.LyDoTuChoi = null;
            poi.NoiDungDeXuat = null;
            poi.MoTaEnDeXuat = null;
            poi.MoTaZhDeXuat = null;
            poi.AudioFileViDeXuat = null;
            poi.AudioFileEnDeXuat = null;
            poi.AudioFileZhDeXuat = null;
            poi.ImagePathDeXuat = null;
            poi.NguoiCapNhat = User.Identity?.Name ?? "admin";

            await _db.SaveChangesAsync(cancellationToken);

            ThemAudioCuCanXoa(audioCuCanXoa, tenFileAudioViCu, poi.TenFileAudio_Vi);
            ThemAudioCuCanXoa(audioCuCanXoa, tenFileAudioEnCu, poi.TenFileAudio_En);
            ThemAudioCuCanXoa(audioCuCanXoa, tenFileAudioZhCu, poi.TenFileAudio_Zh);
            await XoaAudioKhongConSuDungAsync(audioCuCanXoa, cancellationToken);

            TempData["ok"] = $"Đã duyệt đề xuất cho POI {poi.Ten}.";
            return RedirectToAction(nameof(OwnerSubmissions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOwnerSubmission(int id, string? lyDoTuChoi, CancellationToken cancellationToken)
        {
            var poi = await _db.POIs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (poi == null) return NotFound();

            if (!string.Equals(poi.TrangThaiDuyet, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["err"] = "Chỉ có đề xuất Pending mới được từ chối.";
                return RedirectToAction(nameof(OwnerSubmissions));
            }

            poi.TrangThaiDuyet = "Rejected";
            poi.LyDoTuChoi = NormalizeOptionalText(lyDoTuChoi) ?? "Admin từ chối đề xuất.";
            poi.NgayDuyet = DateTime.UtcNow;
            poi.NguoiCapNhat = User.Identity?.Name ?? "admin";

            await _db.SaveChangesAsync(cancellationToken);

            TempData["ok"] = $"Đã từ chối đề xuất cho POI {poi.Ten}.";
            return RedirectToAction(nameof(OwnerSubmissions));
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
            var existingPois = await _db.POIs
                .AsNoTracking()
                .Select(p => new
                {
                    p.Id,
                    p.Ten
                })
                .ToListAsync();

            var existingPoiIds = existingPois
                .Select(p => p.Id)
                .ToList();

            var logs = await _db.PlaybackLogs
                .AsNoTracking()
                .Where(x => existingPoiIds.Contains(x.PoiId))
                .ToListAsync();

            var existingPoiNamesById = existingPois.ToDictionary(p => p.Id, p => p.Ten);

            var topPoi = logs.GroupBy(x => x.PoiId)
                .Select(g => new
                {
                    TenPoi = existingPoiNamesById.TryGetValue(g.Key, out var poiName)
                        ? poiName
                        : g.Select(x => x.PoiTen).FirstOrDefault() ?? string.Empty,
                    SoLan = g.Count()
                })
                .OrderByDescending(x => x.SoLan)
                .Take(10)
                .ToList();

            var lichSuSuDung = logs
                .OrderByDescending(x => x.ThoiGianNghe)
                .Take(30)
                .Select(x => new
                {
                    PoiTen = existingPoiNamesById.TryGetValue(x.PoiId, out var poiName)
                        ? poiName
                        : x.PoiTen,
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
            ViewData["SoPoiCoLuotNghe"] = logs
                .Select(x => x.PoiId)
                .Distinct()
                .Count();
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

        private async Task<bool> TryPopulateTranslatedDescriptionsAsync(
            CmsPoiFormViewModel model,
            PoiModel? existingPoi,
            CancellationToken cancellationToken)
        {
            var resolvedSource = ResolveSubmittedSourceDescription(model, existingPoi);
            var sourceLanguage = resolvedSource.Language;
            var sourceText = resolvedSource.Text;

            model.SourceLanguage = sourceLanguage;
            model.ActiveDescriptionTab = sourceLanguage;
            model.SetDescriptionByLanguage(sourceLanguage, sourceText);

            if (!ShouldTranslateDescriptions(existingPoi, model))
            {
                if (existingPoi != null)
                {
                    model.MoTa_Vi = sourceLanguage == CmsPoiFormViewModel.VietnameseLanguage
                        ? sourceText
                        : existingPoi.MoTa_Vi;
                    model.MoTa_En = sourceLanguage == CmsPoiFormViewModel.EnglishLanguage
                        ? sourceText
                        : existingPoi.MoTa_En;
                    model.MoTa_Zh = sourceLanguage == CmsPoiFormViewModel.ChineseLanguage
                        ? sourceText
                        : existingPoi.MoTa_Zh;
                }

                return true;
            }

            var translationResult = await _translationService.TranslatePoiDescriptionsAsync(
                new PoiDescriptionTranslationRequest(
                    sourceLanguage,
                    sourceText,
                    new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        [CmsPoiFormViewModel.VietnameseLanguage] = model.MoTa_Vi,
                        [CmsPoiFormViewModel.EnglishLanguage] = model.MoTa_En,
                        [CmsPoiFormViewModel.ChineseLanguage] = model.MoTa_Zh
                    }),
                cancellationToken);

            if (!translationResult.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    translationResult.ErrorMessage ?? "Không thể tự động dịch mô tả lúc này.");
                return false;
            }

            ApplyDescriptionsToViewModel(model, translationResult.Descriptions);
            return true;
        }

        private static (string Language, string Text) ResolveSubmittedSourceDescription(
            CmsPoiFormViewModel model,
            PoiModel? existingPoi)
        {
            var requestedSourceLanguage = CmsPoiFormViewModel.NormalizeLanguage(model.SourceLanguage);
            var requestedSourceText = NormalizeText(model.GetDescriptionByLanguage(requestedSourceLanguage));

            if (existingPoi == null)
            {
                if (!string.IsNullOrWhiteSpace(requestedSourceText))
                {
                    return (requestedSourceLanguage, requestedSourceText);
                }

                foreach (var fallbackLanguage in CmsPoiFormViewModel.SupportedLanguages)
                {
                    var fallbackText = NormalizeText(model.GetDescriptionByLanguage(fallbackLanguage));
                    if (!string.IsNullOrWhiteSpace(fallbackText))
                    {
                        return (fallbackLanguage, fallbackText);
                    }
                }

                return (requestedSourceLanguage, requestedSourceText);
            }

            if (HasDescriptionChanged(existingPoi, model, requestedSourceLanguage) &&
                !string.IsNullOrWhiteSpace(requestedSourceText))
            {
                return (requestedSourceLanguage, requestedSourceText);
            }

            foreach (var fallbackLanguage in BuildLanguagePriority(model.ActiveDescriptionTab))
            {
                var fallbackText = NormalizeText(model.GetDescriptionByLanguage(fallbackLanguage));
                if (string.IsNullOrWhiteSpace(fallbackText))
                {
                    continue;
                }

                if (HasDescriptionChanged(existingPoi, model, fallbackLanguage))
                {
                    return (fallbackLanguage, fallbackText);
                }
            }

            return (requestedSourceLanguage, requestedSourceText);
        }

        private static IEnumerable<string> BuildLanguagePriority(string? preferredLanguage)
        {
            var normalizedPreferredLanguage = CmsPoiFormViewModel.NormalizeLanguage(preferredLanguage);
            yield return normalizedPreferredLanguage;

            foreach (var language in CmsPoiFormViewModel.SupportedLanguages)
            {
                if (!language.Equals(normalizedPreferredLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    yield return language;
                }
            }
        }

        private static bool HasDescriptionChanged(
            PoiModel existingPoi,
            CmsPoiFormViewModel model,
            string language)
        {
            var submitted = NormalizeText(model.GetDescriptionByLanguage(language));
            var current = NormalizeText(GetDescriptionByLanguage(existingPoi, language));
            return !string.Equals(submitted, current, StringComparison.Ordinal);
        }

        private static CmsPoiFormViewModel MapToPoiFormViewModel(PoiModel poi)
        {
            var sourceLanguage = ResolveSourceLanguage(poi);

            return new CmsPoiFormViewModel
            {
                Id = poi.Id,
                Ten = poi.Ten,
                MoTa_Vi = poi.MoTa_Vi,
                MoTa_En = poi.MoTa_En,
                MoTa_Zh = poi.MoTa_Zh,
                SourceLanguage = sourceLanguage,
                ActiveDescriptionTab = sourceLanguage,
                Lat = poi.Lat,
                Lng = poi.Lng,
                BanKinh = poi.BanKinh,
                UuTien = poi.UuTien,
                TenFileAnhMinhHoa = poi.TenFileAnhMinhHoa,
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
            };
        }

        private static string ResolveSourceLanguage(PoiModel poi)
        {
            if (!string.IsNullOrWhiteSpace(poi.MoTa_Vi)) return CmsPoiFormViewModel.VietnameseLanguage;
            if (!string.IsNullOrWhiteSpace(poi.MoTa_En)) return CmsPoiFormViewModel.EnglishLanguage;
            if (!string.IsNullOrWhiteSpace(poi.MoTa_Zh)) return CmsPoiFormViewModel.ChineseLanguage;
            return CmsPoiFormViewModel.VietnameseLanguage;
        }

        private static bool ShouldTranslateDescriptions(PoiModel? existingPoi, CmsPoiFormViewModel model)
        {
            var sourceLanguage = CmsPoiFormViewModel.NormalizeLanguage(model.SourceLanguage);
            var submittedSourceText = NormalizeText(model.GetDescriptionByLanguage(sourceLanguage));

            if (string.IsNullOrWhiteSpace(submittedSourceText))
            {
                return false;
            }

            if (existingPoi == null)
            {
                return true;
            }

            var currentSourceText = NormalizeText(GetDescriptionByLanguage(existingPoi, sourceLanguage));

            return !string.Equals(submittedSourceText, currentSourceText, StringComparison.Ordinal) ||
                   string.IsNullOrWhiteSpace(existingPoi.MoTa_Vi) ||
                   string.IsNullOrWhiteSpace(existingPoi.MoTa_En) ||
                   string.IsNullOrWhiteSpace(existingPoi.MoTa_Zh);
        }

        private static string GetDescriptionByLanguage(PoiModel poi, string language) =>
            CmsPoiFormViewModel.NormalizeLanguage(language) switch
            {
                CmsPoiFormViewModel.EnglishLanguage => poi.MoTa_En,
                CmsPoiFormViewModel.ChineseLanguage => poi.MoTa_Zh,
                _ => poi.MoTa_Vi
            };

        private static void ApplyDescriptionsToViewModel(
            CmsPoiFormViewModel model,
            IReadOnlyDictionary<string, string> descriptions)
        {
            model.MoTa_Vi = descriptions.TryGetValue(CmsPoiFormViewModel.VietnameseLanguage, out var vi)
                ? vi?.Trim() ?? string.Empty
                : string.Empty;
            model.MoTa_En = descriptions.TryGetValue(CmsPoiFormViewModel.EnglishLanguage, out var en)
                ? en?.Trim() ?? string.Empty
                : string.Empty;
            model.MoTa_Zh = descriptions.TryGetValue(CmsPoiFormViewModel.ChineseLanguage, out var zh)
                ? zh?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static void ApplyFormToPoi(PoiModel poi, CmsPoiFormViewModel model)
        {
            poi.Ten = NormalizeText(model.Ten);
            poi.MoTa_Vi = NormalizeText(model.MoTa_Vi);
            poi.MoTa_En = NormalizeText(model.MoTa_En);
            poi.MoTa_Zh = NormalizeText(model.MoTa_Zh);
            poi.Lat = model.Lat;
            poi.Lng = model.Lng;
            poi.BanKinh = model.BanKinh;
            poi.UuTien = model.UuTien;
        }

        private static void RestoreCurrentFiles(CmsPoiFormViewModel model, PoiModel? poi)
        {
            if (poi == null) return;
            model.TenFileAnhMinhHoa ??= poi.TenFileAnhMinhHoa;
            model.TenFileAudio_Vi ??= poi.TenFileAudio_Vi;
            model.TenFileAudio_En ??= poi.TenFileAudio_En;
            model.TenFileAudio_Zh ??= poi.TenFileAudio_Zh;
        }

        private static string NormalizeText(string? value) => value?.Trim() ?? string.Empty;

        private static string? NormalizeOptionalText(string? value)
        {
            var normalizedValue = value?.Trim();
            return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
        }

        private static void ThemAudioCuCanXoa(ICollection<string?> audioCanXoa, string? tenFileCu, string? tenFileMoi)
        {
            if (!string.IsNullOrWhiteSpace(tenFileCu) &&
                !string.Equals(tenFileCu, tenFileMoi, StringComparison.OrdinalIgnoreCase))
            {
                audioCanXoa.Add(tenFileCu);
            }
        }

        private static List<string?> LayTatCaAudioCuaPoi(PoiModel poi) =>
            new()
            {
                poi.TenFileAudio_Vi,
                poi.TenFileAudio_En,
                poi.TenFileAudio_Zh,
                poi.AudioFileViDeXuat,
                poi.AudioFileEnDeXuat,
                poi.AudioFileZhDeXuat
            };

        private async Task XoaAudioKhongConSuDungAsync(
            IEnumerable<string?> audioCanXoa,
            CancellationToken cancellationToken = default)
        {
            foreach (var tenFile in audioCanXoa)
            {
                await _audioFileCleanupService.DeleteAudioFileIfUnusedAsync(
                    tenFile,
                    cancellationToken: cancellationToken);
            }
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

        private async Task<string?> LuuFileAudioNeuCo(IFormFile? file, string? tenFileCu, string fieldName)
        {
            if (file == null || file.Length == 0) return tenFileCu;

            var extension = Path.GetExtension(file.FileName);
            var laFileMp3 = string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase);
            var laContentTypeMpeg = string.Equals(file.ContentType, "audio/mpeg", StringComparison.OrdinalIgnoreCase);

            if (!laFileMp3 && !laContentTypeMpeg)
            {
                ModelState.AddModelError(fieldName, "Chỉ cho phép upload file audio MP3.");
                return tenFileCu;
            }

            if (file.Length > KichThuocAudioToiDa)
            {
                ModelState.AddModelError(fieldName, "File audio không được vượt quá 20MB.");
                return tenFileCu;
            }

            var webRootPath = string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : _env.WebRootPath;
            var thuMucAudio = Path.Combine(webRootPath, "audio");
            Directory.CreateDirectory(thuMucAudio);

            var duoiFile = laFileMp3 ? extension.ToLowerInvariant() : ".mp3";
            var tenMoi = $"{Guid.NewGuid():N}{duoiFile}";
            var duongDan = Path.Combine(thuMucAudio, tenMoi);

            try
            {
                await using var stream = System.IO.File.Create(duongDan);
                await file.CopyToAsync(stream);
                return tenMoi;
            }
            catch (Exception)
            {
                ModelState.AddModelError(fieldName, "Không thể lưu file audio. Vui lòng thử lại.");
                return tenFileCu;
            }
        }

        private async Task<string?> CapNhatFileAudioAsync(
            IFormFile? fileMoi,
            string? tenFileCu,
            bool xoaFileCu,
            string fieldName)
        {
            if (fileMoi != null && fileMoi.Length > 0)
            {
                return await LuuFileAudioNeuCo(fileMoi, tenFileCu, fieldName);
            }

            if (xoaFileCu)
            {
                return null;
            }

            return tenFileCu;
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

