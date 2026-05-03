using System.ComponentModel.DataAnnotations;

namespace VinhKhanhApi.ViewModels
{
    public class CmsPoiFormViewModel : IValidatableObject
    {
        public const string VietnameseLanguage = "vi";
        public const string EnglishLanguage = "en";
        public const string ChineseLanguage = "zh";

        private string _sourceLanguage = VietnameseLanguage;
        private string _activeDescriptionTab = VietnameseLanguage;

        public static readonly IReadOnlyList<string> SupportedLanguages =
            [VietnameseLanguage, EnglishLanguage, ChineseLanguage];

        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên địa điểm.")]
        public string Ten { get; set; } = string.Empty;

        public string? MoTa_Vi { get; set; }
        public string? MoTa_En { get; set; }
        public string? MoTa_Zh { get; set; }

        public string SourceLanguage
        {
            get => NormalizeLanguage(_sourceLanguage);
            set => _sourceLanguage = NormalizeLanguage(value);
        }

        public string InputLanguage
        {
            get => SourceLanguage;
            set => SourceLanguage = value;
        }

        public string ActiveDescriptionTab
        {
            get => NormalizeLanguage(_activeDescriptionTab);
            set => _activeDescriptionTab = NormalizeLanguage(value);
        }

        public double Lat { get; set; }
        public double Lng { get; set; }
        public double BanKinh { get; set; } = 50;
        public int UuTien { get; set; } = 5;

        public string? TenFileAnhMinhHoa { get; set; }
        public IFormFile? AnhMinhHoa { get; set; }

        public string? TenFileAudio_Vi { get; set; }
        public string? TenFileAudio_En { get; set; }
        public string? TenFileAudio_Zh { get; set; }
        public IFormFile? AudioVi { get; set; }
        public IFormFile? AudioEn { get; set; }
        public IFormFile? AudioZh { get; set; }
        public bool XoaAudioVi { get; set; }
        public bool XoaAudioEn { get; set; }
        public bool XoaAudioZh { get; set; }

        public string? SoDienThoai { get; set; }
        public string? GioMoCua { get; set; }
        public string? GioDongCua { get; set; }
        public string? MonDacTrung { get; set; }
        public string? GalleryJson { get; set; }
        public string? QrCodeNoiDung { get; set; }
        public string? TtsVoiceCode { get; set; } = "vi-VN";

        public string TrangThaiDuyet { get; set; } = "Approved";
        public string? NoiDungDeXuat { get; set; }
        public DateTime? NgayDeXuat { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }

        public bool IsNew => Id == 0;

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
}
