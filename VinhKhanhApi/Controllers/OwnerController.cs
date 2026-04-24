using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhApi.Data;
using VinhKhanhApi.Models;
using VinhKhanhApi.Services;
using VinhKhanhApi.ViewModels;

namespace VinhKhanhApi.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ITranslationService _translationService;

        public OwnerController(
            AppDbContext db,
            IWebHostEnvironment env,
            ITranslationService translationService)
        {
            _db = db;
            _env = env;
            _translationService = translationService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var poi = await LayPoiTheoOwnerAsync();
            if (poi == null) return Forbid();

            return View(MapToOwnerShopViewModel(poi));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dashboard(OwnerShopViewModel model, CancellationToken cancellationToken)
        {
            var poi = await LayPoiTheoOwnerAsync();
            if (poi == null || poi.Id != model.PoiId) return Forbid();

            model.SourceLanguage = OwnerShopViewModel.NormalizeLanguage(model.SourceLanguage);
            model.ActiveDescriptionTab = model.SourceLanguage;

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

            poi.MoTa_Vi = NormalizeText(model.MoTa_Vi);
            poi.MoTa_En = NormalizeText(model.MoTa_En);
            poi.MoTa_Zh = NormalizeText(model.MoTa_Zh);
            poi.TenFileAnhMinhHoa = await LuuFileAnhNeuCo(model.AnhMinhHoa, poi.TenFileAnhMinhHoa);
            poi.NguoiCapNhat = User.Identity?.Name;

            await _db.SaveChangesAsync(cancellationToken);

            TempData["ok"] = "Đã lưu mô tả và ảnh cho quán.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var poi = await LayPoiTheoOwnerAsync();
            if (poi == null) return Forbid();

            var utcNow = DateTime.UtcNow;
            var vietnamNow = ChuyenSangGioVietNam(utcNow);
            var moc7NgayGanDay = vietnamNow.Date.AddDays(-6);

            var logs = await _db.PlaybackLogs
                .AsNoTracking()
                .Where(x => x.PoiId == poi.Id)
                .OrderByDescending(x => x.ThoiGianNghe)
                .ToListAsync();

            if (logs.Count == 0)
            {
                logs = await _db.PlaybackLogs
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrWhiteSpace(x.PoiTen) && x.PoiTen == poi.Ten)
                    .OrderByDescending(x => x.ThoiGianNghe)
                    .ToListAsync();
            }

            var logsVn = logs
                .Select(log => new
                {
                    Log = log,
                    ThoiGianVietNam = ChuyenSangGioVietNam(log.ThoiGianNghe)
                })
                .ToList();

            var logs7NgayGanDay = logsVn
                .Where(x => x.ThoiGianVietNam.Date >= moc7NgayGanDay)
                .ToList();

            var thuTrongTuan = new[]
            {
                new { Day = DayOfWeek.Monday, Label = "Thá»© 2" },
                new { Day = DayOfWeek.Tuesday, Label = "Thá»© 3" },
                new { Day = DayOfWeek.Wednesday, Label = "Thá»© 4" },
                new { Day = DayOfWeek.Thursday, Label = "Thá»© 5" },
                new { Day = DayOfWeek.Friday, Label = "Thá»© 6" },
                new { Day = DayOfWeek.Saturday, Label = "Thá»© 7" },
                new { Day = DayOfWeek.Sunday, Label = "Chá»§ nháº­t" }
            };

            var model = new OwnerStatsViewModel
            {
                TenQuan = poi.Ten,
                TongLuotNghe = logs.Count,
                ThoiLuongTrungBinhGiay = logs.Count == 0 ? 0 : logs.Average(x => x.ThoiLuongGiay),
                LuotNghe7NgayGanDay = logs7NgayGanDay.Count,
                LuotNgheTheoThu = thuTrongTuan
                    .Select(item => new OwnerStatsDayItemViewModel
                    {
                        ThuLabel = item.Label,
                        SoLuot = logs7NgayGanDay.Count(x => x.ThoiGianVietNam.DayOfWeek == item.Day)
                    })
                    .ToList(),
                LichSuGanDay = logsVn
                    .Take(20)
                    .Select(x => new OwnerRecentPlaybackViewModel
                    {
                        ThoiGianVietNam = x.ThoiGianVietNam,
                        Nguon = x.Log.Nguon,
                        ThoiLuongGiay = x.Log.ThoiLuongGiay
                    })
                    .ToList()
            };

            return View(model);
        }

        private async Task<PoiModel?> LayPoiTheoOwnerAsync()
        {
            var poiId = LayPoiIdTuClaims();
            if (!poiId.HasValue)
            {
                poiId = await LayPoiIdTuTaiKhoanAsync();
            }

            if (!poiId.HasValue)
            {
                return null;
            }

            return await _db.POIs.FirstOrDefaultAsync(x => x.Id == poiId.Value);
        }

        private static OwnerShopViewModel MapToOwnerShopViewModel(PoiModel poi)
        {
            var sourceLanguage = ResolveSourceLanguage(poi);

            return new OwnerShopViewModel
            {
                PoiId = poi.Id,
                Ten = poi.Ten,
                MoTa_Vi = poi.MoTa_Vi,
                MoTa_En = poi.MoTa_En,
                MoTa_Zh = poi.MoTa_Zh,
                SourceLanguage = sourceLanguage,
                ActiveDescriptionTab = sourceLanguage,
                TenFileAnhMinhHoa = poi.TenFileAnhMinhHoa
            };
        }

        private async Task<bool> TryPopulateTranslatedDescriptionsAsync(
            OwnerShopViewModel model,
            PoiModel existingPoi,
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
                model.MoTa_Vi = sourceLanguage == OwnerShopViewModel.VietnameseLanguage
                    ? sourceText
                    : existingPoi.MoTa_Vi;
                model.MoTa_En = sourceLanguage == OwnerShopViewModel.EnglishLanguage
                    ? sourceText
                    : existingPoi.MoTa_En;
                model.MoTa_Zh = sourceLanguage == OwnerShopViewModel.ChineseLanguage
                    ? sourceText
                    : existingPoi.MoTa_Zh;

                return true;
            }

            var translationResult = await _translationService.TranslatePoiDescriptionsAsync(
                new PoiDescriptionTranslationRequest(
                    sourceLanguage,
                    sourceText,
                    new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        [OwnerShopViewModel.VietnameseLanguage] = model.MoTa_Vi,
                        [OwnerShopViewModel.EnglishLanguage] = model.MoTa_En,
                        [OwnerShopViewModel.ChineseLanguage] = model.MoTa_Zh
                    }),
                cancellationToken);

            if (!translationResult.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    translationResult.ErrorMessage ?? "KhÃ´ng thá»ƒ tá»± Ä‘á»™ng dá»‹ch mÃ´ táº£ lÃºc nÃ y.");
                return false;
            }

            ApplyDescriptionsToViewModel(model, translationResult.Descriptions);
            return true;
        }

        private static (string Language, string Text) ResolveSubmittedSourceDescription(
            OwnerShopViewModel model,
            PoiModel existingPoi)
        {
            var requestedSourceLanguage = OwnerShopViewModel.NormalizeLanguage(model.SourceLanguage);
            var requestedSourceText = NormalizeText(model.GetDescriptionByLanguage(requestedSourceLanguage));

            if (HasDescriptionChanged(existingPoi, model, requestedSourceLanguage) &&
                !string.IsNullOrWhiteSpace(requestedSourceText))
            {
                return (requestedSourceLanguage, requestedSourceText);
            }

            foreach (var fallbackLanguage in BuildLanguagePriority(model.ActiveDescriptionTab))
            {
                var fallbackText = NormalizeText(model.GetDescriptionByLanguage(fallbackLanguage));
                if (string.IsNullOrWhiteSpace(fallbackText))
                    continue;

                if (HasDescriptionChanged(existingPoi, model, fallbackLanguage))
                    return (fallbackLanguage, fallbackText);
            }

            return (requestedSourceLanguage, requestedSourceText);
        }

        private static IEnumerable<string> BuildLanguagePriority(string? preferredLanguage)
        {
            var normalizedPreferredLanguage = OwnerShopViewModel.NormalizeLanguage(preferredLanguage);
            yield return normalizedPreferredLanguage;

            foreach (var language in OwnerShopViewModel.SupportedLanguages)
            {
                if (!language.Equals(normalizedPreferredLanguage, StringComparison.OrdinalIgnoreCase))
                    yield return language;
            }
        }

        private static bool HasDescriptionChanged(PoiModel existingPoi, OwnerShopViewModel model, string language)
        {
            var submitted = NormalizeText(model.GetDescriptionByLanguage(language));
            var current = NormalizeText(GetDescriptionByLanguage(existingPoi, language));
            return !string.Equals(submitted, current, StringComparison.Ordinal);
        }

        private static bool ShouldTranslateDescriptions(PoiModel existingPoi, OwnerShopViewModel model)
        {
            var sourceLanguage = OwnerShopViewModel.NormalizeLanguage(model.SourceLanguage);
            var submittedSourceText = NormalizeText(model.GetDescriptionByLanguage(sourceLanguage));

            if (string.IsNullOrWhiteSpace(submittedSourceText))
                return false;

            var currentSourceText = NormalizeText(GetDescriptionByLanguage(existingPoi, sourceLanguage));

            return !string.Equals(submittedSourceText, currentSourceText, StringComparison.Ordinal) ||
                   string.IsNullOrWhiteSpace(existingPoi.MoTa_Vi) ||
                   string.IsNullOrWhiteSpace(existingPoi.MoTa_En) ||
                   string.IsNullOrWhiteSpace(existingPoi.MoTa_Zh);
        }

        private static string GetDescriptionByLanguage(PoiModel poi, string language) =>
            OwnerShopViewModel.NormalizeLanguage(language) switch
            {
                OwnerShopViewModel.EnglishLanguage => poi.MoTa_En,
                OwnerShopViewModel.ChineseLanguage => poi.MoTa_Zh,
                _ => poi.MoTa_Vi
            };

        private static void ApplyDescriptionsToViewModel(
            OwnerShopViewModel model,
            IReadOnlyDictionary<string, string> descriptions)
        {
            model.MoTa_Vi = descriptions.TryGetValue(OwnerShopViewModel.VietnameseLanguage, out var vi)
                ? vi?.Trim() ?? string.Empty
                : string.Empty;
            model.MoTa_En = descriptions.TryGetValue(OwnerShopViewModel.EnglishLanguage, out var en)
                ? en?.Trim() ?? string.Empty
                : string.Empty;
            model.MoTa_Zh = descriptions.TryGetValue(OwnerShopViewModel.ChineseLanguage, out var zh)
                ? zh?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static void RestoreCurrentFiles(OwnerShopViewModel model, PoiModel poi)
        {
            model.TenFileAnhMinhHoa ??= poi.TenFileAnhMinhHoa;
        }

        private static string ResolveSourceLanguage(PoiModel poi)
        {
            if (!string.IsNullOrWhiteSpace(poi.MoTa_Vi)) return OwnerShopViewModel.VietnameseLanguage;
            if (!string.IsNullOrWhiteSpace(poi.MoTa_En)) return OwnerShopViewModel.EnglishLanguage;
            if (!string.IsNullOrWhiteSpace(poi.MoTa_Zh)) return OwnerShopViewModel.ChineseLanguage;
            return OwnerShopViewModel.VietnameseLanguage;
        }

        private static string NormalizeText(string? value) => value?.Trim() ?? string.Empty;

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

        private int? LayPoiIdTuClaims()
        {
            var value = User.FindFirstValue("poi_id");
            if (int.TryParse(value, out var poiId)) return poiId;
            return null;
        }

        private async Task<int?> LayPoiIdTuTaiKhoanAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            return await _db.UserAccounts
                .AsNoTracking()
                .Where(x => x.Username == username)
                .Select(x => x.PoiId)
                .FirstOrDefaultAsync();
        }

        private static DateTime ChuyenSangGioVietNam(DateTime utcDateTime)
        {
            var normalizedUtc = utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, VietnamTimeZone);
        }

        private static TimeZoneInfo ResolveVietnamTimeZone()
        {
            foreach (var id in new[] { "SE Asia Standard Time", "Asia/Bangkok" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
        }
    }
}

