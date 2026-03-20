using Android.App;
using Android.Runtime;

namespace App
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();
            // Thay YOUR_GOOGLE_MAPS_API_KEY bằng key thật khi có
            Microsoft.Maui.Controls.Maps.GoogleMapsService.ProvideAPIKey("YOUR_GOOGLE_MAPS_API_KEY");
        }
    }
}