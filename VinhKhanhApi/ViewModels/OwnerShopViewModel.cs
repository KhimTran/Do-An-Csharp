using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VinhKhanhApi.ViewModels
{
    public class OwnerShopViewModel : IValidatableObject
    {
        public const string VietnameseLanguage = "vi";
        public const string EnglishLanguage = "en";
        public const string ChineseLanguage = "zh";

        private string _sourceLanguage = VietnameseLanguage;
        private string _activeDescriptionTab = VietnameseLanguage;

        public static readonly IReadOnlyList<string> SupportedLanguages =
            [VietnameseLanguage, EnglishLanguage, ChineseLanguage];

        public int PoiId { get; set; }
        public string Ten { get; set; } = string.Empty;

        public string MoTa_Vi { get; set; } = string.Empty;
        public string MoTa_En { get; set; } = string.Empty;
        public string MoTa_Zh { get; set; } = string.Empty;

        public string SourceLanguage
        {
            get => NormalizeLanguage(_sourceLanguage);
            set => _sourceLanguage = NormalizeLanguage(value);
        }

        public string ActiveDescriptionTab
        {
            get => NormalizeLanguage(_activeDescriptionTab);
            set => _activeDescriptionTab = NormalizeLanguage(value);
        }

        public string? TenFileAnhMinhHoa { get; set; }
        public string? TenFileAudio_Vi { get; set; }
        public string? TenFileAudio_En { get; set; }
        public string? TenFileAudio_Zh { get; set; }
        public string? AudioFileViDeXuat { get; set; }
        public string? AudioFileEnDeXuat { get; set; }
        public string? AudioFileZhDeXuat { get; set; }
        public string? ImagePathDeXuat { get; set; }
        public string TrangThaiDuyet { get; set; } = "Approved";
        public DateTime? NgayDeXuat { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }

        public IFormFile? AnhMinhHoa { get; set; }
        public IFormFile? AudioVi { get; set; }
        public IFormFile? AudioEn { get; set; }
        public IFormFile? AudioZh { get; set; }
        public bool XoaAudioVi { get; set; }
        public bool XoaAudioEn { get; set; }
        public bool XoaAudioZh { get; set; }

        public string SourceDescriptionFieldName => GetDescriptionFieldName(SourceLanguage);

        public string GetDescriptionByLanguage(string language) =>
            NormalizeLanguage(language) switch
            {
                EnglishLanguage => MoTa_En ?? string.Empty,
                ChineseLanguage => MoTa_Zh ?? string.Empty,
                _ => MoTa_Vi ?? string.Empty
            };

        public void SetDescriptionByLanguage(string language, string? value)
        {
            var normalizedValue = value?.Trim() ?? string.Empty;

            switch (NormalizeLanguage(language))
            {
                case EnglishLanguage:
                    MoTa_En = normalizedValue;
                    break;
                case ChineseLanguage:
                    MoTa_Zh = normalizedValue;
                    break;
                default:
                    MoTa_Vi = normalizedValue;
                    break;
            }
        }

        public static string NormalizeLanguage(string? language) =>
            language?.Trim().ToLowerInvariant() switch
            {
                EnglishLanguage => EnglishLanguage,
                ChineseLanguage or "zh-cn" or "cn" => ChineseLanguage,
                _ => VietnameseLanguage
            };

        public static string GetDescriptionFieldName(string language) =>
            NormalizeLanguage(language) switch
            {
                EnglishLanguage => nameof(MoTa_En),
                ChineseLanguage => nameof(MoTa_Zh),
                _ => nameof(MoTa_Vi)
            };

        public static string GetDisplayNameForLanguage(string language) =>
            NormalizeLanguage(language) switch
            {
                EnglishLanguage => "English",
                ChineseLanguage => "中文",
                _ => "Tiếng Việt"
            };

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!SupportedLanguages.Contains(SourceLanguage))
            {
                yield return new ValidationResult(
                    "Ngôn ngữ nguồn không hợp lệ.",
                    [nameof(SourceLanguage)]);
            }

            if (string.IsNullOrWhiteSpace(GetDescriptionByLanguage(SourceLanguage)))
            {
                yield return new ValidationResult(
                    "Vui lòng nhập mô tả cho ngôn ngữ nguồn đã chọn.",
                    [SourceDescriptionFieldName, nameof(SourceLanguage)]);
            }
        }
    }

    public class OwnerStatsViewModel
    {
        public string TenQuan { get; set; } = string.Empty;
        public int TongLuotNghe { get; set; }
        public double ThoiLuongTrungBinhGiay { get; set; }
        public int LuotNghe7NgayGanDay { get; set; }
        public List<OwnerStatsDayItemViewModel> LuotNgheTheoThu { get; set; } = [];
        public List<OwnerRecentPlaybackViewModel> LichSuGanDay { get; set; } = [];
    }

    public class OwnerStatsDayItemViewModel
    {
        public string ThuLabel { get; set; } = string.Empty;
        public int SoLuot { get; set; }
    }

    public class OwnerRecentPlaybackViewModel
    {
        public DateTime ThoiGianVietNam { get; set; }
        public string Nguon { get; set; } = string.Empty;
        public int ThoiLuongGiay { get; set; }
    }
}
