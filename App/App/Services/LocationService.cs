namespace App.Services;

public class LocationService : ILocationService
{
    private static readonly TimeSpan TrackingInterval = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan TrackingTimeout = TimeSpan.FromSeconds(12);

    private CancellationTokenSource? _cts;
    private LocationSnapshot? _lastEmittedLocation;

    public async Task BatDauTheoDoiAsync(
        Action<LocationSnapshot> khiCoViTri,
        Action<LocationTrackingStatus>? khiTrangThaiThayDoi = null)
    {
        if (_cts != null && !_cts.IsCancellationRequested)
            return;

        var trangThai = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (trangThai != PermissionStatus.Granted)
            trangThai = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (trangThai != PermissionStatus.Granted)
        {
            if (TryEmitVirtualFallback(khiCoViTri, khiTrangThaiThayDoi))
                return;

            khiTrangThaiThayDoi?.Invoke(new LocationTrackingStatus(
                LocationTrackingState.PermissionDenied,
                "Location permission was denied."));
            return;
        }

        StartAndroidForegroundService();

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var viTriGanNhat = await LayViTriHienTaiAsync();
        if (viTriGanNhat != null)
        {
            _lastEmittedLocation = viTriGanNhat;
            khiCoViTri(viTriGanNhat);
        }

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var viTri = await Geolocation.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Best, TrackingTimeout),
                        token);

                    if (viTri == null)
                    {
                        if (TryEmitVirtualFallback(khiCoViTri, khiTrangThaiThayDoi))
                            continue;

                        khiTrangThaiThayDoi?.Invoke(new LocationTrackingStatus(
                            LocationTrackingState.Error,
                            "Location is currently unavailable."));
                    }
                    else
                    {
                        var snapshot = new LocationSnapshot(
                            viTri.Latitude,
                            viTri.Longitude,
                            viTri.Accuracy,
                            viTri.Timestamp);

                        if (ShouldEmit(snapshot))
                        {
                            _lastEmittedLocation = snapshot;
                            khiCoViTri(snapshot);
                        }

                        khiTrangThaiThayDoi?.Invoke(new LocationTrackingStatus(LocationTrackingState.Tracking));
                    }
                }
                catch (FeatureNotEnabledException)
                {
                    if (TryEmitVirtualFallback(khiCoViTri, khiTrangThaiThayDoi))
                        continue;

                    khiTrangThaiThayDoi?.Invoke(new LocationTrackingStatus(
                        LocationTrackingState.Disabled,
                        "GPS is disabled."));
                }
                catch (PermissionException)
                {
                    if (TryEmitVirtualFallback(khiCoViTri, khiTrangThaiThayDoi))
                        continue;

                    khiTrangThaiThayDoi?.Invoke(new LocationTrackingStatus(
                        LocationTrackingState.PermissionDenied,
                        "Location permission was denied."));
                    break;
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GPS] Lỗi: {ex.Message}");
                    khiTrangThaiThayDoi?.Invoke(new LocationTrackingStatus(
                        LocationTrackingState.Error,
                        ex.Message));
                }

                try
                {
                    await Task.Delay(TrackingInterval, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    public void DungTheoDoi()
    {
        if (_cts == null)
            return;

        try
        {
            _cts.Cancel();
        }
        catch
        {
        }

        _cts.Dispose();
        _cts = null;
        _lastEmittedLocation = null;
        StopAndroidForegroundService();
    }

    public async Task<LocationSnapshot?> LayViTriHienTaiAsync()
    {
        try
        {
            var viTriHienTai = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TrackingTimeout));

            if (viTriHienTai != null)
            {
                var snapshot = TaoSnapshot(viTriHienTai);
                GhiLogSnapshot("current", snapshot);

                if (LocationSnapshotValidation.IsUsableForDisplay(snapshot))
                    return snapshot;

                GhiLogBoQua("current", snapshot, "not-usable-for-display");
            }
        }
        catch (Exception ex) when (ex is FeatureNotEnabledException
                                   or FeatureNotSupportedException
                                   or PermissionException
                                   or TaskCanceledException
                                   or OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[GPS] Current location unavailable: {ex.GetType().Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GPS] Current location error: {ex.Message}");
        }

        try
        {
            var viTriGanNhat = await Geolocation.GetLastKnownLocationAsync();
            if (viTriGanNhat != null)
            {
                var snapshot = TaoSnapshot(viTriGanNhat);
                GhiLogSnapshot("last-known", snapshot);

                if (LocationSnapshotValidation.IsTrustedForGeofence(snapshot, out var reason))
                    return snapshot;

                GhiLogBoQua("last-known", snapshot, reason);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GPS] Last-known location unavailable: {ex.GetType().Name}: {ex.Message}");
        }

        return DeviceRuntimeProfile.IsVirtualDevice
            ? DeviceRuntimeProfile.CreateDemoLocation()
            : null;
    }

    private bool TryEmitVirtualFallback(
        Action<LocationSnapshot> khiCoViTri,
        Action<LocationTrackingStatus>? khiTrangThaiThayDoi)
    {
        if (!DeviceRuntimeProfile.IsVirtualDevice)
            return false;

        var snapshot = DeviceRuntimeProfile.CreateDemoLocation();
        if (ShouldEmit(snapshot))
        {
            _lastEmittedLocation = snapshot;
            khiCoViTri(snapshot);
        }

        khiTrangThaiThayDoi?.Invoke(new LocationTrackingStatus(
            LocationTrackingState.Simulated,
            "Using a simulated emulator location."));

        return true;
    }

    private bool ShouldEmit(LocationSnapshot snapshot)
    {
        if (_lastEmittedLocation == null)
            return true;

        var khoangCach = GeofenceService.TinhKhoangCachMetres(
            _lastEmittedLocation.Lat,
            _lastEmittedLocation.Lng,
            snapshot.Lat,
            snapshot.Lng);

        return khoangCach >= 3;
    }

    private static LocationSnapshot TaoSnapshot(Location location) => new(
        location.Latitude,
        location.Longitude,
        location.Accuracy,
        location.Timestamp);

    private static void GhiLogSnapshot(string source, LocationSnapshot snapshot)
    {
        var age = LocationSnapshotValidation.GetAge(snapshot);
        var ageText = age.HasValue ? $"{age.Value.TotalSeconds:0.#}s" : "n/a";
        var accuracyText = snapshot.AccuracyMeters.HasValue ? $"{snapshot.AccuracyMeters.Value:0.#}m" : "n/a";

        System.Diagnostics.Debug.WriteLine(
            $"[GPS] source={source}, lat={snapshot.Lat:0.000000}, lng={snapshot.Lng:0.000000}, accuracy={accuracyText}, age={ageText}");
    }

    private static void GhiLogBoQua(string source, LocationSnapshot snapshot, string reason)
    {
        var age = LocationSnapshotValidation.GetAge(snapshot);
        var ageText = age.HasValue ? $"{age.Value.TotalSeconds:0.#}s" : "n/a";
        var accuracyText = snapshot.AccuracyMeters.HasValue ? $"{snapshot.AccuracyMeters.Value:0.#}m" : "n/a";

        System.Diagnostics.Debug.WriteLine(
            $"[GPS] Skip {source} location: reason={reason}, lat={snapshot.Lat:0.000000}, lng={snapshot.Lng:0.000000}, accuracy={accuracyText}, age={ageText}");
    }

    private static void StartAndroidForegroundService()
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(context, typeof(global::App.LocationForegroundService));

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
                context.StartForegroundService(intent);
            else
                context.StartService(intent);

            System.Diagnostics.Debug.WriteLine("[GPS] Android foreground service started.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GPS] Could not start Android foreground service: {ex.Message}");
        }
#endif
    }

    private static void StopAndroidForegroundService()
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(context, typeof(global::App.LocationForegroundService));
            context.StopService(intent);

            System.Diagnostics.Debug.WriteLine("[GPS] Android foreground service stopped.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GPS] Could not stop Android foreground service: {ex.Message}");
        }
#endif
    }
}
