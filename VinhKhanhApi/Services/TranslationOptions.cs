namespace VinhKhanhApi.Services
{
    public sealed class TranslationOptions
    {
        public const string SectionName = "Translation";

        public string Provider { get; set; } = "GoogleTranslateAjax";
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public bool UseGoogleFallback { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 8;
    }
}
