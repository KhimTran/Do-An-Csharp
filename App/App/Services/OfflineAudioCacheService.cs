using App.Models;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace App.Services
{
    public class OfflineAudioCacheService : IOfflineAudioCacheService
    {
        private const string CacheFolderName = "offline-audio";
        private readonly LocalDatabase _db;
        private readonly HttpClient _http;

        public OfflineAudioCacheService(LocalDatabase db)
        {
            _db = db;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<OfflineAudioCacheResult> DownloadAllPoiAudioAsync(
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var cacheDirectory = GetCacheDirectory();
            Directory.CreateDirectory(cacheDirectory);

            var danhSachPoi = await _db.LayTatCaPoiAsync();
            var candidates = danhSachPoi
                .SelectMany(BuildCandidates)
                .Where(c => IsMp3File(c.ServerFileName))
                .ToList();

            var downloaded = 0;
            var skipped = 0;
            var failed = 0;

            progress?.Report(candidates.Count == 0
                ? "Khong co file MP3 de tai."
                : $"Dang tai 0/{candidates.Count} file...");

            for (var index = 0; index < candidates.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var candidate = candidates[index];
                var displayIndex = index + 1;
                var localPath = BuildLocalPath(cacheDirectory, candidate.Poi.Id, candidate.Language, candidate.ServerFileName);

                progress?.Report($"Dang tai {displayIndex}/{candidates.Count}: {candidate.Poi.Ten}");

                try
                {
                    RemoveStaleLanguageCache(candidate.Poi, candidate.Language, localPath);

                    if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
                    {
                        SetLocalPath(candidate.Poi, candidate.Language, localPath);
                        candidate.Poi.LocalAudioCachedAt ??= DateTimeOffset.UtcNow.ToString("O");
                        await _db.LuuPoiAsync(candidate.Poi);
                        skipped++;
                        continue;
                    }

                    var audioUrl = ApiEndpointResolver.BuildPoiAudioUrl(candidate.ServerFileName);
                    if (string.IsNullOrWhiteSpace(audioUrl))
                    {
                        failed++;
                        Debug.WriteLine($"[OfflineAudio] Khong tao duoc audio URL cho POI {candidate.Poi.Id}: {candidate.ServerFileName}");
                        continue;
                    }

                    if (!await DownloadFileAsync(audioUrl, localPath, cancellationToken))
                    {
                        failed++;
                        continue;
                    }

                    SetLocalPath(candidate.Poi, candidate.Language, localPath);
                    candidate.Poi.LocalAudioCachedAt = DateTimeOffset.UtcNow.ToString("O");
                    await _db.LuuPoiAsync(candidate.Poi);
                    downloaded++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    Debug.WriteLine($"[OfflineAudio] Loi tai audio POI {candidate.Poi.Id} ({candidate.Language}): {ex.Message}");
                }
            }

            var cacheSize = await GetCacheSizeBytesAsync();
            progress?.Report($"Da tai {downloaded}/{candidates.Count} file, bo qua {skipped}, loi {failed}.");

            return new OfflineAudioCacheResult(candidates.Count, downloaded, skipped, failed, cacheSize);
        }

        public async Task ClearCacheAsync()
        {
            var cacheDirectory = GetCacheDirectory();

            try
            {
                if (Directory.Exists(cacheDirectory))
                    Directory.Delete(cacheDirectory, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OfflineAudio] Loi xoa cache audio: {ex.Message}");
            }

            Directory.CreateDirectory(cacheDirectory);
            await _db.XoaTatCaDuongDanAudioLocalAsync();
        }

        public Task<long> GetCacheSizeBytesAsync()
        {
            try
            {
                var cacheDirectory = GetCacheDirectory();
                if (!Directory.Exists(cacheDirectory))
                    return Task.FromResult(0L);

                long total = Directory
                    .EnumerateFiles(cacheDirectory, "*", SearchOption.AllDirectories)
                    .Sum(path =>
                    {
                        try
                        {
                            return new FileInfo(path).Length;
                        }
                        catch
                        {
                            return 0L;
                        }
                    });

                return Task.FromResult(total);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OfflineAudio] Loi tinh dung luong cache: {ex.Message}");
                return Task.FromResult(0L);
            }
        }

        public string? GetCachedAudioPath(PoiModel poi, string language)
        {
            var localPath = NormalizeLanguage(language) switch
            {
                "en" => poi.LocalAudioPath_En,
                "zh" => poi.LocalAudioPath_Zh,
                _ => poi.LocalAudioPath_Vi
            };

            if (string.IsNullOrWhiteSpace(localPath))
                return null;

            try
            {
                return File.Exists(localPath) ? localPath : null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> DownloadFileAsync(string audioUrl, string localPath, CancellationToken cancellationToken)
        {
            var tempPath = $"{localPath}.tmp";

            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                using var response = await _http.GetAsync(audioUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[OfflineAudio] HTTP {(int)response.StatusCode} khi tai {audioUrl}");
                    return false;
                }

                await using (var remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var localStream = File.Create(tempPath))
                {
                    await remoteStream.CopyToAsync(localStream, cancellationToken);
                }

                if (!File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
                {
                    TryDeleteFile(tempPath);
                    Debug.WriteLine($"[OfflineAudio] File tai ve rong: {audioUrl}");
                    return false;
                }

                if (File.Exists(localPath))
                    File.Delete(localPath);

                File.Move(tempPath, localPath);
                return true;
            }
            catch (OperationCanceledException)
            {
                TryDeleteFile(tempPath);
                throw;
            }
            catch (Exception ex)
            {
                TryDeleteFile(tempPath);
                Debug.WriteLine($"[OfflineAudio] Loi tai {audioUrl}: {ex.Message}");
                return false;
            }
        }

        private static IEnumerable<AudioDownloadCandidate> BuildCandidates(PoiModel poi)
        {
            if (!string.IsNullOrWhiteSpace(poi.TenFileAudio_Vi))
                yield return new AudioDownloadCandidate(poi, "vi", poi.TenFileAudio_Vi.Trim());

            if (!string.IsNullOrWhiteSpace(poi.TenFileAudio_En))
                yield return new AudioDownloadCandidate(poi, "en", poi.TenFileAudio_En.Trim());

            if (!string.IsNullOrWhiteSpace(poi.TenFileAudio_Zh))
                yield return new AudioDownloadCandidate(poi, "zh", poi.TenFileAudio_Zh.Trim());
        }

        private static bool IsMp3File(string serverFileName)
        {
            var fileName = ExtractOriginalFileName(serverFileName);
            return fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildLocalPath(string cacheDirectory, int poiId, string language, string serverFileName)
        {
            var safeFileName = MakeSafeFileName(ExtractOriginalFileName(serverFileName));
            var normalizedLanguage = NormalizeLanguage(language);
            var localFileName = $"poi_{poiId}_{normalizedLanguage}_{safeFileName}";
            return Path.Combine(cacheDirectory, localFileName);
        }

        private static string ExtractOriginalFileName(string serverFileName)
        {
            var raw = serverFileName.Trim();
            if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
            {
                var fromUri = Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrWhiteSpace(fromUri))
                    return Uri.UnescapeDataString(fromUri);
            }

            var markerIndex = raw.IndexOfAny(new[] { '?', '#' });
            if (markerIndex >= 0)
                raw = raw[..markerIndex];

            var fileName = Path.GetFileName(raw.TrimStart('/'));
            return string.IsNullOrWhiteSpace(fileName) ? "audio.mp3" : fileName;
        }

        private static string MakeSafeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safe = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
            return string.IsNullOrWhiteSpace(safe) ? "audio.mp3" : safe;
        }

        private static void RemoveStaleLanguageCache(PoiModel poi, string language, string expectedPath)
        {
            var currentPath = NormalizeLanguage(language) switch
            {
                "en" => poi.LocalAudioPath_En,
                "zh" => poi.LocalAudioPath_Zh,
                _ => poi.LocalAudioPath_Vi
            };

            if (string.IsNullOrWhiteSpace(currentPath) ||
                string.Equals(currentPath, expectedPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TryDeleteFile(currentPath);
            SetLocalPath(poi, language, null);
        }

        private static void SetLocalPath(PoiModel poi, string language, string? localPath)
        {
            switch (NormalizeLanguage(language))
            {
                case "en":
                    poi.LocalAudioPath_En = localPath;
                    break;
                case "zh":
                    poi.LocalAudioPath_Zh = localPath;
                    break;
                default:
                    poi.LocalAudioPath_Vi = localPath;
                    break;
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OfflineAudio] Khong xoa duoc file {path}: {ex.Message}");
            }
        }

        private static string NormalizeLanguage(string language)
            => PoiDescriptionResolver.NormalizeLanguage(language);

        private static string GetCacheDirectory()
            => Path.Combine(FileSystem.AppDataDirectory, CacheFolderName);

        private sealed record AudioDownloadCandidate(PoiModel Poi, string Language, string ServerFileName);
    }
}
