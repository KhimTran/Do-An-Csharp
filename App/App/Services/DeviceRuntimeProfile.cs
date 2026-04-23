using Microsoft.Maui.Devices;

namespace App.Services;

public static class DeviceRuntimeProfile
{
    private static readonly LocationSnapshot EmulatorDemoLocation = new(
        10.76082,
        106.70442,
        5,
        DateTimeOffset.UtcNow);

    public static bool IsVirtualDevice => DeviceInfo.DeviceType == DeviceType.Virtual;

    public static LocationSnapshot CreateDemoLocation() => EmulatorDemoLocation with
    {
        Timestamp = DateTimeOffset.UtcNow
    };
}
