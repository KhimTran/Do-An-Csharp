namespace App.Services;

public enum LocationTrackingState
{
    Idle,
    Tracking,
    PermissionDenied,
    Disabled,
    Error
}

public sealed record LocationSnapshot(
    double Lat,
    double Lng,
    double? AccuracyMeters = null,
    DateTimeOffset? Timestamp = null);

public sealed record LocationTrackingStatus(
    LocationTrackingState State,
    string? Details = null);
