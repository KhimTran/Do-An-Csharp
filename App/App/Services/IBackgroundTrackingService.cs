namespace App.Services
{
    public interface IBackgroundTrackingService
    {
        Task StartAsync();
        Task StopAsync();
    }
}
