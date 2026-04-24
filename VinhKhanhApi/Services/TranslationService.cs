using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace VinhKhanhApi.Services
{
    public sealed class TranslationService : ITranslationService
    {
        private const string GoogleTranslateAjaxProvider = "GoogleTranslateAjax";
        private const string LibreTranslateProvider = "LibreTranslate";
        private static readonly string[] SupportedLanguages = ["vi", "en", "zh"];

        private readonly HttpClient _httpClient;
        private readonly ILogger<TranslationService> _logger;
        private readonly TranslationOptions _options;

        public TranslationService(
            HttpClient httpClient,
            IOptions<TranslationOptions> options,
            ILogger<TranslationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value ?? new TranslationOptions();
            _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 3, 30));
        }

        public async Task<PoiDescriptionTranslationResult> TranslatePoiDescriptionsAsync(
            PoiDescriptionTranslationRequest request,
            CancellationToken cancellationToken = default)
        {
            var sourceLanguage = NormalizeLanguage(request.SourceLanguage);
            var sourceText = request.SourceText?.Trim();

            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return new PoiDescriptionTranslationResult
                {
                    Success = false,
                    ErrorMessage = "Vui lòng nhập mô tả cho ngôn ngữ nguồn trước khi lưu."
                };
            }

            var descriptions = SupportedLanguages.ToDictionary(
                language => language,
                language => request.ExistingDescriptions.TryGetValue(language, out var existing)
                    ? existing?.Trim() ?? string.Empty
                    : string.Empty,
                StringComparer.OrdinalIgnoreCase);

            descriptions[sourceLanguage] = sourceText;

            try
            {
                foreach (var targetLanguage in SupportedLanguages.Where(language =>
                             !language.Equals(sourceLanguage, StringComparison.OrdinalIgnoreCase)))
                {
                    descriptions[targetLanguage] = await TranslateTextWithFallbackAsync(
                        sourceText,
                        sourceLanguage,
                        targetLanguage,
                        cancellationToken);
                }

                return new PoiDescriptionTranslationResult
                {
                    Success = true,
                    Descriptions = new Dictionary<string, string>(descriptions, StringComparer.OrdinalIgnoreCase)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Automatic translation failed for CMS POI form from {SourceLanguage}",
                    sourceLanguage);

                return new PoiDescriptionTranslationResult
                {
                    Success = false,
                    ErrorMessage = "Không thể tự động dịch mô tả lúc này. Nội dung gốc vẫn được giữ lại, vui lòng kiểm tra cấu hình dịch hoặc thử lại sau.",
                    Descriptions = new Dictionary<string, string>(descriptions, StringComparer.OrdinalIgnoreCase)
                };
            }
        }

        private async Task<string> TranslateTextWithFallbackAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            CancellationToken cancellationToken)
        {
            if (sourceLanguage.Equals(targetLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }

            foreach (var provider in BuildProviderChain())
            {
                try
                {
                    var translatedText = provider switch
                    {
                        LibreTranslateProvider => await TranslateWithLibreTranslateAsync(
                            text,
                            sourceLanguage,
                            targetLanguage,
                            cancellationToken),
                        _ => await TranslateWithGoogleTranslateAjaxAsync(
                            text,
                            sourceLanguage,
                            targetLanguage,
                            cancellationToken)
                    };

                    if (!string.IsNullOrWhiteSpace(translatedText))
                    {
                        return translatedText.Trim();
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
                {
                    _logger.LogInformation(
                        ex,
                        "Translation provider {Provider} failed for {SourceLanguage}->{TargetLanguage}",
                        provider,
                        sourceLanguage,
                        targetLanguage);
                }
            }

            throw new InvalidOperationException("No translation provider returned a valid result.");
        }

        private IReadOnlyList<string> BuildProviderChain()
        {
            var primaryProvider = NormalizeProvider(_options.Provider);
            var providers = new List<string> { primaryProvider };

            // Defaulting to Google's public endpoint keeps the classroom demo simple because
            // it works without provisioning secrets. The provider remains configurable so the
            // project can move to LibreTranslate or another backend later.
            if (_options.UseGoogleFallback &&
                !primaryProvider.Equals(GoogleTranslateAjaxProvider, StringComparison.OrdinalIgnoreCase))
            {
                providers.Add(GoogleTranslateAjaxProvider);
            }

            return providers;
        }

        private async Task<string> TranslateWithLibreTranslateAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                throw new InvalidOperationException("LibreTranslate BaseUrl is not configured.");
            }

            var payload = new Dictionary<string, string>
            {
                ["q"] = text,
                ["source"] = MapLibreTranslateLanguage(sourceLanguage),
                ["target"] = MapLibreTranslateLanguage(targetLanguage),
                ["format"] = "text"
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                payload["api_key"] = _options.ApiKey;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl.TrimEnd('/')}/translate")
            {
                Content = JsonContent.Create(payload)
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty("translatedText", out var translatedTextElement))
            {
                var translatedText = translatedTextElement.GetString();
                if (!string.IsNullOrWhiteSpace(translatedText))
                {
                    return translatedText;
                }
            }

            throw new InvalidOperationException("LibreTranslate returned an empty result.");
        }

        private async Task<string> TranslateWithGoogleTranslateAjaxAsync(
            string text,
            string sourceLanguage,
            string targetLanguage,
            CancellationToken cancellationToken)
        {
            var translatedChunks = new List<string>();

            foreach (var chunk in SplitTextIntoChunks(text))
            {
                var url =
                    $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={MapGoogleLanguage(sourceLanguage)}&tl={MapGoogleLanguage(targetLanguage)}&dt=t&q={Uri.EscapeDataString(chunk)}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("User-Agent", "VinhKhanhCMS/1.0");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                translatedChunks.Add(ExtractGoogleTranslatedText(json));
            }

            return string.Join(Environment.NewLine + Environment.NewLine, translatedChunks)
                .Trim();
        }

        private static IReadOnlyList<string> SplitTextIntoChunks(string text)
        {
            const int maxChunkLength = 1200;

            var normalizedText = text.Replace("\r\n", "\n").Trim();
            if (normalizedText.Length <= maxChunkLength)
            {
                return [normalizedText];
            }

            var chunks = new List<string>();
            var remainingText = normalizedText;

            while (remainingText.Length > 0)
            {
                var currentLength = Math.Min(maxChunkLength, remainingText.Length);
                var chunkLength = currentLength == remainingText.Length
                    ? currentLength
                    : FindBreakIndex(remainingText, currentLength);

                var chunk = remainingText[..chunkLength].Trim();
                if (string.IsNullOrWhiteSpace(chunk))
                {
                    chunkLength = currentLength;
                    chunk = remainingText[..chunkLength].Trim();
                }

                chunks.Add(chunk);
                remainingText = remainingText[chunkLength..].TrimStart();
            }

            return chunks;
        }

        private static int FindBreakIndex(string text, int preferredLength)
        {
            var minimumLength = preferredLength / 2;

            for (var index = preferredLength - 1; index >= minimumLength; index--)
            {
                if (char.IsWhiteSpace(text[index]) || text[index] is '.' or '!' or '?' or ';' or ',')
                {
                    return index + 1;
                }
            }

            return preferredLength;
        }

        private static string ExtractGoogleTranslatedText(string json)
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Google Translate returned an unexpected payload.");
            }

            var segments = new StringBuilder();
            foreach (var part in document.RootElement[0].EnumerateArray())
            {
                if (part.ValueKind != JsonValueKind.Array || part.GetArrayLength() == 0)
                {
                    continue;
                }

                var translatedSegment = part[0].GetString();
                if (!string.IsNullOrWhiteSpace(translatedSegment))
                {
                    segments.Append(translatedSegment);
                }
            }

            var translatedText = segments.ToString().Trim();
            return !string.IsNullOrWhiteSpace(translatedText)
                ? translatedText
                : throw new InvalidOperationException("Google Translate returned an empty result.");
        }

        private static string NormalizeLanguage(string? language) =>
            language?.Trim().ToLowerInvariant() switch
            {
                "en" => "en",
                "zh" or "zh-cn" or "cn" => "zh",
                _ => "vi"
            };

        private static string NormalizeProvider(string? provider)
        {
            if (string.Equals(provider, LibreTranslateProvider, StringComparison.OrdinalIgnoreCase))
            {
                return LibreTranslateProvider;
            }

            return GoogleTranslateAjaxProvider;
        }

        private static string MapLibreTranslateLanguage(string language) => NormalizeLanguage(language);

        private static string MapGoogleLanguage(string language) =>
            NormalizeLanguage(language) switch
            {
                "zh" => "zh-CN",
                _ => NormalizeLanguage(language)
            };
    }
}
