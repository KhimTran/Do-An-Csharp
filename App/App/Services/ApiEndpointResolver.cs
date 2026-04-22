using Microsoft.Maui.Storage;

namespace App.Services;

public static class ApiEndpointResolver
{
    private const string ApiBaseUrlPreferenceKey = "api_base_url";

    public static IEnumerable<string> GetBaseUrls()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configuredBaseUrl = GetConfiguredBaseUrl();

        if (!string.IsNullOrWhiteSpace(configuredBaseUrl) && seen.Add(configuredBaseUrl))
            yield return configuredBaseUrl;
    }

    public static IEnumerable<string> GetPoiApiUrls()
    {
        foreach (var baseUrl in GetBaseUrls())
            yield return $"{baseUrl}/api/pois";
    }

    public static string? GetConfiguredBaseUrl()
    {
        var customBaseUrl = Preferences.Get(ApiBaseUrlPreferenceKey, string.Empty)?.Trim();
        if (string.IsNullOrWhiteSpace(customBaseUrl))
            return null;

        return NormalizeBaseUrl(customBaseUrl);
    }

    public static bool HasConfiguredBaseUrl() =>
        !string.IsNullOrWhiteSpace(GetConfiguredBaseUrl());

    public static string NormalizeBaseUrl(string url)
    {
        var normalized = url.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"http://{normalized}";
        }

        return normalized;
    }

    public static string? BuildPoiImageUrl(string? tenFileAnh)
    {
        if (string.IsNullOrWhiteSpace(tenFileAnh))
            return null;

        var raw = tenFileAnh.Trim();
        if (Uri.TryCreate(raw, UriKind.Absolute, out var absoluteUri))
            return absoluteUri.ToString();

        var baseUrl = GetConfiguredBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        return $"{baseUrl}/images/poi/{raw}";
    }
}
