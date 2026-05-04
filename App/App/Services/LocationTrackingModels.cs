namespace App.Services;

public enum LocationTrackingState
{
    Idle,
    Tracking,
    Simulated,
    PermissionDenied,
    Disabled,
    Error
}

public sealed record LocationSnapshot(
    double Lat,
    double Lng,
    double? AccuracyMeters = null,
    DateTimeOffset? Timestamp = null);

public static class LocationSnapshotValidation
{
    public static readonly TimeSpan MaxTrustedAge = TimeSpan.FromSeconds(60);
    public const double MaxTrustedAccuracyMeters = 100;
    public const double MaxDisplayAccuracyMeters = 1_000;

    public static bool HasUsableCoordinates(LocationSnapshot snapshot) =>
        double.IsFinite(snapshot.Lat)
        && double.IsFinite(snapshot.Lng)
        && snapshot.Lat is >= -90 and <= 90
        && snapshot.Lng is >= -180 and <= 180;

    public static bool IsUsableForDisplay(LocationSnapshot snapshot) =>
        HasUsableCoordinates(snapshot)
        && (!snapshot.AccuracyMeters.HasValue || snapshot.AccuracyMeters.Value <= MaxDisplayAccuracyMeters);

    public static bool IsTrustedForGeofence(LocationSnapshot snapshot, out string reason)
    {
        if (!HasUsableCoordinates(snapshot))
        {
            reason = "invalid-coordinates";
            return false;
        }

        if (!snapshot.Timestamp.HasValue)
        {
            reason = "missing-timestamp";
            return false;
        }

        var age = GetAge(snapshot);
        if (!age.HasValue)
        {
            reason = "missing-timestamp";
            return false;
        }

        if (age.Value > MaxTrustedAge)
        {
            reason = $"stale-age-{age.Value.TotalSeconds:0.#}s";
            return false;
        }

        if (!snapshot.AccuracyMeters.HasValue)
        {
            reason = "missing-accuracy";
            return false;
        }

        if (snapshot.AccuracyMeters.Value > MaxTrustedAccuracyMeters)
        {
            reason = $"low-accuracy-{snapshot.AccuracyMeters.Value:0.#}m";
            return false;
        }

        reason = "trusted";
        return true;
    }

    public static TimeSpan? GetAge(LocationSnapshot snapshot)
    {
        if (!snapshot.Timestamp.HasValue)
            return null;

        var age = DateTimeOffset.UtcNow - snapshot.Timestamp.Value.ToUniversalTime();
        return age < TimeSpan.Zero ? TimeSpan.Zero : age;
    }
}

public sealed record LocationTrackingStatus(
    LocationTrackingState State,
    string? Details = null);
