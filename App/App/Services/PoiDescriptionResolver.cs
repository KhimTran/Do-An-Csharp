using App.Models;

namespace App.Services
{
    public static class PoiDescriptionResolver
    {
        public static string GetBestDescription(PoiModel poi, string? preferredLanguage)
        {
            return GetBestDescriptionWithLanguage(poi, preferredLanguage).Text;
        }

        public static (string Language, string Text) GetBestDescriptionWithLanguage(PoiModel poi, string? preferredLanguage)
        {
            foreach (var language in BuildLanguagePriority(preferredLanguage))
            {
                var content = language switch
                {
                    "en" => poi.MoTa_En,
                    "zh" => poi.MoTa_Zh,
                    _ => poi.MoTa_Vi
                };

                if (!string.IsNullOrWhiteSpace(content))
                {
                    return (language, content.Trim());
                }
            }

            return (NormalizeLanguage(preferredLanguage), string.Empty);
        }

        public static string GetBestDescriptionOrDefault(PoiModel poi, string? preferredLanguage, string fallbackMessage)
        {
            var content = GetBestDescription(poi, preferredLanguage);
            return string.IsNullOrWhiteSpace(content) ? fallbackMessage : content;
        }

        public static string NormalizeLanguage(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return "vi";
            }

            var normalized = language.Trim().ToLowerInvariant();
            if (normalized.StartsWith("en", StringComparison.Ordinal))
            {
                return "en";
            }

            if (normalized.StartsWith("zh", StringComparison.Ordinal))
            {
                return "zh";
            }

            return "vi";
        }

        private static IEnumerable<string> BuildLanguagePriority(string? preferredLanguage)
        {
            switch (NormalizeLanguage(preferredLanguage))
            {
                case "en":
                    yield return "en";
                    yield return "vi";
                    yield return "zh";
                    break;
                case "zh":
                    yield return "zh";
                    yield return "vi";
                    yield return "en";
                    break;
                default:
                    yield return "vi";
                    yield return "en";
                    yield return "zh";
                    break;
            }
        }
    }
}
