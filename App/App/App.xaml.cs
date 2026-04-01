using App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace App
{
    public partial class App : Application
    {
        public App(SyncService sync)
        {
            InitializeComponent();
            // Chạy đồng bộ POI ngầm khi app khởi động
            // Không await để không chặn UI
            Task.Run(async () => await sync.DongBoPoisAsync());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}