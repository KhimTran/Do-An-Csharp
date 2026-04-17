namespace App.Services
{
    // Fallback cho nền tảng chưa triển khai foreground service.
    public class NoopBackgroundTrackingService : IBackgroundTrackingService
    {
        public Task StartAsync() => Task.CompletedTask;
        public Task StopAsync() => Task.CompletedTask;
    }
}
