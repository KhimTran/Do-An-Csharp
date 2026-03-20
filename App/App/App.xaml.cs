using Microsoft.Extensions.DependencyInjection;

namespace App
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

#if ANDROID
        protected override async void OnStart()
        {
            base.OnStart();
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return;

            var intent = new Android.Content.Intent(
                Android.App.Application.Context,
                typeof(LocationForegroundService)
            );
            Android.App.Application.Context.StartForegroundService(intent);
        }
#endif
    }
}