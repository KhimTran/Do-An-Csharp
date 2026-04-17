// Platforms/Android/LocationForegroundService.cs
using Android.App;
using Android.Content;
using Android.OS;
using App.Models;
using App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;

namespace App
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationForegroundService : Android.App.Service
    {
        private const string ChannelId = "location_channel";
        private const int ForegroundNotificationId = 1001;

        private CancellationTokenSource? _cts;
        private GeofenceService? _geofence;
        private ITtsService? _tts;
        private bool _dangXuLy;

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            BatDauNotificationNen("Đang theo dõi vị trí nền...");
            KhoiDongDependencyNeuCan();
            KhoiDongVongLapNenNeuCan();
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            base.OnDestroy();
        }

        private void KhoiDongDependencyNeuCan()
        {
            if (_geofence != null && _tts != null) return;

            var services = MauiProgram.Services;
            if (services == null) return;

            _geofence = services.GetService(typeof(GeofenceService)) as GeofenceService;
            _tts = services.GetService(typeof(ITtsService)) as ITtsService;
        }

        private void KhoiDongVongLapNenNeuCan()
        {
            if (_cts != null && !_cts.IsCancellationRequested) return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (_dangXuLy || _geofence == null || _tts == null)
                        {
                            await Task.Delay(3000, token);
                            continue;
                        }

                        _dangXuLy = true;

                        var location = await Geolocation.GetLocationAsync(
                            new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)),
                            token);

                        if (location != null)
                        {
                            var poi = await _geofence.KiemTraVungAsync(location.Latitude, location.Longitude);
                            if (poi != null)
                            {
                                var maNgonNgu = Preferences.Get("tts_language", "vi-VN");
                                var noiDung = ChonNoiDungTheoNgonNgu(poi, maNgonNgu);
                                if (!string.IsNullOrWhiteSpace(noiDung))
                                    await _tts.PhatAmAsync(noiDung, maNgonNgu);

                                BatDauNotificationNen($"Đang gần {poi.Ten} • geofence trigger");
                            }
                        }
                    }
                    catch (FeatureNotEnabledException)
                    {
                        BatDauNotificationNen("GPS đang tắt. Bật GPS để tiếp tục geofence.");
                    }
                    catch (PermissionException)
                    {
                        BatDauNotificationNen("Thiếu quyền vị trí nền. Vui lòng cấp lại quyền.");
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // Không crash service khi lỗi nền.
                    }
                    finally
                    {
                        _dangXuLy = false;
                    }

                    try
                    {
                        await Task.Delay(5000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        private void BatDauNotificationNen(string contentText)
        {
            var manager = (NotificationManager)GetSystemService(NotificationService)!;
            var channel = new NotificationChannel(ChannelId, "Theo dõi vị trí nền", NotificationImportance.Low);
            manager.CreateNotificationChannel(channel);

            var notification = new Notification.Builder(this, ChannelId)
                .SetContentTitle("Vĩnh Khánh Audio Guide")
                .SetContentText(contentText)
                .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
                .SetOngoing(true)
                .Build();

            StartForeground(ForegroundNotificationId, notification);
        }

        private static string ChonNoiDungTheoNgonNgu(PoiModel poi, string maNgonNgu)
        {
            if (maNgonNgu.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(poi.MoTa_En) ? poi.MoTa_Vi : poi.MoTa_En;

            if (maNgonNgu.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(poi.MoTa_Zh) ? poi.MoTa_Vi : poi.MoTa_Zh;

            return poi.MoTa_Vi;
        }
    }
}
