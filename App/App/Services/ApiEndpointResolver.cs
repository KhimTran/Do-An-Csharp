using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace App.Services;

public static class ApiEndpointResolver
{
    private const string ApiBaseUrlPreferenceKey = "api_base_url";

    private static readonly string[] DefaultBaseUrls =
    {
        "http://10.0.2.2:5099", // Android Emulator
        "http://localhost:5099" // Windows/local
    };

    public static IEnumerable<string> GetBaseUrls()
    {
        var customBaseUrl = Preferences.Get(ApiBaseUrlPreferenceKey, string.Empty)?.Trim();
        if (!string.IsNullOrWhiteSpace(customBaseUrl))
            yield return NormalizeBaseUrl(customBaseUrl);

        foreach (var baseUrl in DefaultBaseUrls)
            yield return NormalizeBaseUrl(baseUrl);
    }

    public static IEnumerable<string> GetPoiApiUrls()
    {
        foreach (var baseUrl in GetBaseUrls())
            yield return $"{baseUrl}/api/pois";
    }

    public static string NormalizeBaseUrl(string url)
    {
        var normalized = url.Trim().TrimEnd('/');
        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"http://{normalized}";
        }

        if (DeviceInfo.Platform == DevicePlatform.Android)
            return normalized.Replace("localhost", "10.0.2.2", StringComparison.OrdinalIgnoreCase);

        return normalized;
    }

    public static string GetConfiguredBaseUrlOrDefault()
    {
        var customBaseUrl = Preferences.Get(ApiBaseUrlPreferenceKey, string.Empty)?.Trim();
        if (!string.IsNullOrWhiteSpace(customBaseUrl))
            return NormalizeBaseUrl(customBaseUrl);

        return NormalizeBaseUrl(DefaultBaseUrls[0]);
    }
}
