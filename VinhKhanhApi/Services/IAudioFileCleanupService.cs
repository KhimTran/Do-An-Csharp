namespace VinhKhanhApi.Services
{
    public interface IAudioFileCleanupService
    {
        Task DeleteAudioFileIfUnusedAsync(
            string? fileName,
            int? excludingPoiId = null,
            CancellationToken cancellationToken = default);
    }
}
