using App.Models;

namespace App.Services
{
    public sealed record OfflineAudioCacheResult(
        int TotalFiles,
        int Downloaded,
        int Skipped,
        int Failed,
        long CacheSizeBytes)
    {
        public string Summary =>
            LocalizationResourceManager.Instance.Translate(
                "SettingsPage_AudioOfflineDownloadSummaryFormat",
                Downloaded,
                TotalFiles,
                Skipped,
                Failed);
    }

    public interface IOfflineAudioCacheService
    {
        Task<OfflineAudioCacheResult> DownloadAllPoiAudioAsync(
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);

        Task ClearCacheAsync();

        Task<long> GetCacheSizeBytesAsync();

        string? GetCachedAudioPath(PoiModel poi, string language);
    }
}
