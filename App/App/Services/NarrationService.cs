using App.Models;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class NarrationService : INarrationService
    {
        private readonly ITtsService _tts;
        private readonly IAudioPlaybackService _audioPlayback;
        private readonly IOfflineAudioCacheService _offlineAudioCache;

        public NarrationService(
            ITtsService tts,
            IAudioPlaybackService audioPlayback,
            IOfflineAudioCacheService offlineAudioCache)
        {
            _tts = tts;
            _audioPlayback = audioPlayback;
            _offlineAudioCache = offlineAudioCache;
        }

        public async Task<NarrationPlaybackResult> PhatThuyetMinhPoiAsync(
            PoiModel poi,
            string? maNgonNgu = null,
            CancellationToken cancellationToken = default)
        {
            var requestedLanguage = ResolveLanguage(maNgonNgu);
            var description = PoiDescriptionResolver.GetBestDescriptionWithLanguage(poi, requestedLanguage);
            var offlineMode = Preferences.Get("offline_mode", false);

            foreach (var language in BuildAudioLanguagePriority(requestedLanguage))
            {
                var localPath = _offlineAudioCache.GetCachedAudioPath(poi, language);
                if (!string.IsNullOrWhiteSpace(localPath))
                {
                    _tts.DungPhat();
                    if (await _audioPlayback.PlayAsync(localPath, cancellationToken))
                        return NarrationPlaybackResult.CompletedAudio(language, description.Text);
                }

                if (offlineMode)
                    continue;

                var fileName = GetAudioFileName(poi, language);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    _tts.DungPhat();

                    var audioUrl = ApiEndpointResolver.BuildPoiAudioUrl(fileName);
                    if (!string.IsNullOrWhiteSpace(audioUrl) &&
                        await _audioPlayback.PlayAsync(audioUrl, cancellationToken))
                    {
                        return NarrationPlaybackResult.CompletedAudio(language, description.Text);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(description.Text))
            {
                await _audioPlayback.StopAsync();
                return NarrationPlaybackResult.Empty(requestedLanguage);
            }

            await _audioPlayback.StopAsync();
            var ttsLanguage = ToTtsLanguageCode(description.Language);
            var contentKey = StringComparer.Ordinal.GetHashCode(description.Text.Trim()).ToString("X");
            var audioKey = $"poi:{poi.Id}:{description.Language}:{contentKey}";

            var ttsResult = await _tts.PhatAmAsync(
                description.Text,
                ttsLanguage,
                audioKey,
                poi.Ten);

            return NarrationPlaybackResult.FromTts(ttsResult, description.Language, description.Text);
        }

        private static string ResolveLanguage(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                language = Preferences.Get("app_language", Preferences.Get("tts_language", "vi-VN"));
            }

            return PoiDescriptionResolver.NormalizeLanguage(language);
        }

        private static string? GetAudioFileName(PoiModel poi, string language)
        {
            var fileName = language switch
            {
                "en" => poi.TenFileAudio_En,
                "zh" => poi.TenFileAudio_Zh,
                _ => poi.TenFileAudio_Vi
            };

            return string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
        }

        private static IEnumerable<string> BuildAudioLanguagePriority(string requestedLanguage)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (seen.Add(requestedLanguage))
                yield return requestedLanguage;

            foreach (var language in new[] { "vi", "en", "zh" })
            {
                if (seen.Add(language))
                    yield return language;
            }
        }

        private static string ToTtsLanguageCode(string language) => language switch
        {
            "en" => "en-US",
            "zh" => "zh-CN",
            _ => "vi-VN"
        };
    }
}
