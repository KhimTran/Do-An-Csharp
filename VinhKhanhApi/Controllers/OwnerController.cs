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
        private const string StatusPending = "Pending";
        private const string StatusApproved = "Approved";
        private const string StatusRejected = "Rejected";
        private const long KichThuocAudioToiDa = 20 * 1024 * 1024;
        private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ITranslationService _translationService;
        private readonly IAudioFileCleanupService _audioFileCleanupService;

        public OwnerController(
            AppDbContext db,
            IWebHostEnvironment env,
            ITranslationService translationService,
            IAudioFileCleanupService audioFileCleanupService)
        {
            _db = db;
            _env = env;
            _translationService = translationService;
            _audioFileCleanupService = audioFileCleanupService;
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

            var audioDeXuatCuCanXoa = new List<string?>();
            var tenFileAudioViDeXuatCu = poi.AudioFileViDeXuat;
            var tenFileAudioEnDeXuatCu = poi.AudioFileEnDeXuat;
            var tenFileAudioZhDeXuatCu = poi.AudioFileZhDeXuat;

            var imagePathDeXuat = await LuuFileAnhNeuCo(model.AnhMinhHoa, poi.ImagePathDeXuat);
            var tenFileAudioVi = await CapNhatFileAudioAsync(
                model.AudioVi,
                poi.AudioFileViDeXuat,
                model.XoaAudioVi,
                nameof(OwnerShopViewModel.AudioVi));
            var tenFileAudioEn = await CapNhatFileAudioAsync(
                model.AudioEn,
                poi.AudioFileEnDeXuat,
                model.XoaAudioEn,
                nameof(OwnerShopViewModel.AudioEn));
            var tenFileAudioZh = await CapNhatFileAudioAsync(
                model.AudioZh,
                poi.AudioFileZhDeXuat,
                model.XoaAudioZh,
                nameof(OwnerShopViewModel.AudioZh));

            if (!ModelState.IsValid)
            {
                model.ImagePathDeXuat = imagePathDeXuat;
                model.AudioFileViDeXuat = tenFileAudioVi;
                model.AudioFileEnDeXuat = tenFileAudioEn;
                model.AudioFileZhDeXuat = tenFileAudioZh;
                RestoreCurrentFiles(model, poi);
                return View(model);
            }

            poi.NoiDungDeXuat = NormalizeProposalText(model.MoTa_Vi, poi.MoTa_Vi);
            poi.MoTaEnDeXuat = NormalizeProposalText(model.MoTa_En, poi.MoTa_En);
            poi.MoTaZhDeXuat = NormalizeProposalText(model.MoTa_Zh, poi.MoTa_Zh);
            poi.ImagePathDeXuat = imagePathDeXuat;
            poi.AudioFileViDeXuat = tenFileAudioVi;
            poi.AudioFileEnDeXuat = tenFileAudioEn;
            poi.AudioFileZhDeXuat = tenFileAudioZh;
            if (!IsApproved(poi.TrangThaiDuyet))
                poi.TrangThaiDuyet = StatusPending;

            poi.TrangThaiDeXuatOwner = StatusPending;
            poi.NgayDeXuat = DateTime.UtcNow;
            poi.NgayDuyet = null;
            poi.LyDoTuChoi = null;
            poi.NguoiCapNhat = User.Identity?.Name;

            await _db.SaveChangesAsync(cancellationToken);

            ThemAudioCuCanXoa(audioDeXuatCuCanXoa, tenFileAudioViDeXuatCu, poi.AudioFileViDeXuat);
            ThemAudioCuCanXoa(audioDeXuatCuCanXoa, tenFileAudioEnDeXuatCu, poi.AudioFileEnDeXuat);
            ThemAudioCuCanXoa(audioDeXuatCuCanXoa, tenFileAudioZhDeXuatCu, poi.AudioFileZhDeXuat);
            await XoaAudioKhongConSuDungAsync(audioDeXuatCuCanXoa, cancellationToken);

            TempData["ok"] = "Đã gửi đề xuất thay đổi. Vui lòng chờ Admin duyệt trước khi nội dung hiển thị công khai.";
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
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            };

            var model = new OwnerStatsViewModel
            {
                TenQuan = poi.Ten,
                TongLuotNghe = logs.Count,
                ThoiLuongTrungBinhGiay = logs.Count == 0 ? 0 : logs.Average(x => x.ThoiLuongGiay),
                LuotNghe7NgayGanDay = logs7NgayGanDay.Count,
                LuotNgheTheoThu = thuTrongTuan
                    .Select(day => new OwnerStatsDayItemViewModel
                    {
                        ThuLabel = GetVietnameseDayLabel(day),
                        SoLuot = logs7NgayGanDay.Count(x => x.ThoiGianVietNam.DayOfWeek == day)
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
                MoTa_Vi = GetDescriptionByLanguage(poi, OwnerShopViewModel.VietnameseLanguage),
                MoTa_En = GetDescriptionByLanguage(poi, OwnerShopViewModel.EnglishLanguage),
                MoTa_Zh = GetDescriptionByLanguage(poi, OwnerShopViewModel.ChineseLanguage),
                SourceLanguage = sourceLanguage,
                ActiveDescriptionTab = sourceLanguage,
                TenFileAnhMinhHoa = poi.TenFileAnhMinhHoa,
                TenFileAudio_Vi = poi.TenFileAudio_Vi,
                TenFileAudio_En = poi.TenFileAudio_En,
                TenFileAudio_Zh = poi.TenFileAudio_Zh,
                AudioFileViDeXuat = poi.AudioFileViDeXuat,
                AudioFileEnDeXuat = poi.AudioFileEnDeXuat,
                AudioFileZhDeXuat = poi.AudioFileZhDeXuat,
                ImagePathDeXuat = poi.ImagePathDeXuat,
                TrangThaiDuyet = poi.TrangThaiDuyet,
                TrangThaiDeXuatOwner = poi.TrangThaiDeXuatOwner,
                NgayDeXuat = poi.NgayDeXuat,
                NgayDuyet = poi.NgayDuyet,
                LyDoTuChoi = poi.LyDoTuChoi
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
                    : GetDescriptionByLanguage(existingPoi, OwnerShopViewModel.VietnameseLanguage);
                model.MoTa_En = sourceLanguage == OwnerShopViewModel.EnglishLanguage
                    ? sourceText
                    : GetDescriptionByLanguage(existingPoi, OwnerShopViewModel.EnglishLanguage);
                model.MoTa_Zh = sourceLanguage == OwnerShopViewModel.ChineseLanguage
                    ? sourceText
                    : GetDescriptionByLanguage(existingPoi, OwnerShopViewModel.ChineseLanguage);

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
                OwnerShopViewModel.EnglishLanguage => UseOwnerProposal(poi, poi.MoTaEnDeXuat, poi.MoTa_En),
                OwnerShopViewModel.ChineseLanguage => UseOwnerProposal(poi, poi.MoTaZhDeXuat, poi.MoTa_Zh),
                _ => UseOwnerProposal(poi, poi.NoiDungDeXuat, poi.MoTa_Vi)
            };

        private static string UseOwnerProposal(PoiModel poi, string? proposedValue, string publicValue)
        {
            if (HasVisibleOwnerProposal(poi) && !string.IsNullOrWhiteSpace(proposedValue))
            {
                return proposedValue.Trim();
            }

            return publicValue;
        }

        private static bool HasVisibleOwnerProposal(PoiModel poi) =>
            string.Equals(poi.TrangThaiDeXuatOwner, StatusPending, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(poi.TrangThaiDeXuatOwner, StatusRejected, StringComparison.OrdinalIgnoreCase);

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
            model.TenFileAudio_Vi ??= poi.TenFileAudio_Vi;
            model.TenFileAudio_En ??= poi.TenFileAudio_En;
            model.TenFileAudio_Zh ??= poi.TenFileAudio_Zh;
            model.ImagePathDeXuat ??= poi.ImagePathDeXuat;
            model.AudioFileViDeXuat ??= poi.AudioFileViDeXuat;
            model.AudioFileEnDeXuat ??= poi.AudioFileEnDeXuat;
            model.AudioFileZhDeXuat ??= poi.AudioFileZhDeXuat;
            model.TrangThaiDuyet = poi.TrangThaiDuyet;
            model.TrangThaiDeXuatOwner = poi.TrangThaiDeXuatOwner;
            model.NgayDeXuat = poi.NgayDeXuat;
            model.NgayDuyet = poi.NgayDuyet;
            model.LyDoTuChoi = poi.LyDoTuChoi;
        }

        private static string ResolveSourceLanguage(PoiModel poi)
        {
            if (!string.IsNullOrWhiteSpace(poi.MoTa_Vi)) return OwnerShopViewModel.VietnameseLanguage;
            if (!string.IsNullOrWhiteSpace(poi.MoTa_En)) return OwnerShopViewModel.EnglishLanguage;
            if (!string.IsNullOrWhiteSpace(poi.MoTa_Zh)) return OwnerShopViewModel.ChineseLanguage;
            return OwnerShopViewModel.VietnameseLanguage;
        }

        private static string NormalizeText(string? value) => value?.Trim() ?? string.Empty;

        private static bool IsApproved(string? status) =>
            string.Equals(status, StatusApproved, StringComparison.OrdinalIgnoreCase);

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

        private static string? NormalizeProposalText(string? proposedValue, string currentPublicValue)
        {
            var normalizedProposal = NormalizeOptionalText(proposedValue);
            if (normalizedProposal == null)
            {
                return null;
            }

            return string.Equals(normalizedProposal, NormalizeText(currentPublicValue), StringComparison.Ordinal)
                ? null
                : normalizedProposal;
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

        private static string GetVietnameseDayLabel(DayOfWeek dayOfWeek) => dayOfWeek switch
        {
            DayOfWeek.Monday => "Th\u1EE9 2",
            DayOfWeek.Tuesday => "Th\u1EE9 3",
            DayOfWeek.Wednesday => "Th\u1EE9 4",
            DayOfWeek.Thursday => "Th\u1EE9 5",
            DayOfWeek.Friday => "Th\u1EE9 6",
            DayOfWeek.Saturday => "Th\u1EE9 7",
            DayOfWeek.Sunday => "Ch\u1EE7 nh\u1EADt",
            _ => dayOfWeek.ToString()
        };

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

