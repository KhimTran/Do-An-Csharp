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
            khiTrangThaiThayDoi?.Invoke(new LocationTrackingStatus(
                LocationTrackingState.PermissionDenied,
                "Location permission was denied."));
            return;
        }

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
                    khiTrangThaiThayDoi?.Invoke(new LocationTrackingStatus(
                        LocationTrackingState.Disabled,
                        "GPS is disabled."));
                }
                catch (PermissionException)
                {
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
    }

    public async Task<LocationSnapshot?> LayViTriHienTaiAsync()
    {
        var viTri = await Geolocation.GetLastKnownLocationAsync();
        if (viTri == null)
            return null;

        return new LocationSnapshot(
            viTri.Latitude,
            viTri.Longitude,
            viTri.Accuracy,
            viTri.Timestamp);
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
}
