namespace App.Services
{
    public interface IAudioPlaybackService
    {
        Task<bool> PlayAsync(string audioSource, CancellationToken cancellationToken = default);

        Task StopAsync();
    }
}
