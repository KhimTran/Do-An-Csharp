namespace VinhKhanhApi.Services
{
    public interface ITranslationService
    {
        Task<PoiDescriptionTranslationResult> TranslatePoiDescriptionsAsync(
            PoiDescriptionTranslationRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed record PoiDescriptionTranslationRequest(
        string SourceLanguage,
        string SourceText,
        IReadOnlyDictionary<string, string?> ExistingDescriptions);

    public sealed class PoiDescriptionTranslationResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public IReadOnlyDictionary<string, string> Descriptions { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
