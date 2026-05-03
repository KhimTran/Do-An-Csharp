namespace App.Services
{
    public interface IAudioPlaybackService
    {
        Task<bool> PlayAsync(string audioUrl, CancellationToken cancellationToken = default);

        Task StopAsync();
    }
}
