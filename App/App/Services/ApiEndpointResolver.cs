using System.Net;
using Microsoft.Maui.Storage;

namespace App.Services;

public static class ApiEndpointResolver
{
    private const string ApiBaseUrlPreferenceKey = "api_base_url";
    private const string DefaultNgrokBaseUrl = "https://tabby-swimmer-reflex.ngrok-free.dev";
    private const string PoiApiPath = "api/pois";

    public static IEnumerable<string> GetBaseUrls()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configuredBaseUrl = GetLegacyConfiguredBaseUrl();
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl) && seen.Add(configuredBaseUrl))
            yield return configuredBaseUrl;

        var defaultBaseUrl = GetDefaultBaseUrl();
        if (!string.IsNullOrWhiteSpace(defaultBaseUrl) && seen.Add(defaultBaseUrl))
            yield return defaultBaseUrl;
    }

    public static IEnumerable<string> GetPoiApiUrls()
    {
        foreach (var baseUrl in GetBaseUrls())
        {
            var poiApiUrl = BuildPoiApiUrl(baseUrl);
            if (!string.IsNullOrWhiteSpace(poiApiUrl))
                yield return poiApiUrl;
        }
    }

    public static string? GetConfiguredBaseUrl()
        => GetBaseUrls().FirstOrDefault();

    public static bool HasConfiguredBaseUrl() =>
        !string.IsNullOrWhiteSpace(GetConfiguredBaseUrl());

    public static bool TryNormalizeApiBaseUrl(string? url, out string normalizedBaseUrl, out string poiApiUrl, out string errorMessage)
    {
        normalizedBaseUrl = string.Empty;
        poiApiUrl = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            errorMessage = "QR khong chua URL server.";
            return false;
        }

        var candidate = AppendSchemeIfMissing(url.Trim());
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var rawUri))
        {
            errorMessage = "URL server khong hop le.";
            return false;
        }

        if (!rawUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !rawUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "URL server phai dung http hoac https.";
            return false;
        }

        if (LooksLikeBinaryDownload(rawUri.AbsolutePath))
        {
            errorMessage = "URL server dang tro toi file cai dat, khong phai API.";
            return false;
        }

        if (!IsSupportedQrApiPath(rawUri.AbsolutePath))
        {
            errorMessage = "URL server chi nen tro toi host goc, /api hoac /api/pois.";
            return false;
        }

        normalizedBaseUrl = NormalizeBaseUrl(candidate);
        if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out _))
        {
            errorMessage = "Khong the chuan hoa URL server tu QR.";
            normalizedBaseUrl = string.Empty;
            return false;
        }

        poiApiUrl = BuildPoiApiUrl(normalizedBaseUrl);
        return true;
    }

    public static string NormalizeBaseUrl(string url)
    {
        var normalized = url.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        normalized = AppendSchemeIfMissing(normalized.TrimEnd('/'));

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            return normalized.TrimEnd('/');

        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
            Path = NormalizeBasePath(uri.AbsolutePath)
        };

        return builder.Uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    public static bool IsLoopbackBaseUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var normalized = NormalizeBaseUrl(url);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            return false;

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(uri.Host, out var address) &&
               IPAddress.IsLoopback(address);
    }

    public static string? BuildPoiImageUrl(string? tenFileAnh)
        => BuildPoiImageUrl(GetConfiguredBaseUrl(), tenFileAnh);

    public static string? BuildPoiAudioUrl(string? tenFileAudio)
        => BuildPoiAudioUrl(GetConfiguredBaseUrl(), tenFileAudio);

    public static string? BuildPoiImageUrl(string? baseUrl, string? tenFileAnh)
    {
        if (string.IsNullOrWhiteSpace(tenFileAnh))
            return null;

        var raw = tenFileAnh.Trim();
        if (Uri.TryCreate(raw, UriKind.Absolute, out var absoluteUri))
            return absoluteUri.ToString();

        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedBaseUrl) ||
            !Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out _))
        {
            return null;
        }

        return BuildAbsoluteUrl(normalizedBaseUrl, $"images/poi/{raw.TrimStart('/')}");
    }

    public static string? BuildPoiAudioUrl(string? baseUrl, string? tenFileAudio)
    {
        if (string.IsNullOrWhiteSpace(tenFileAudio))
            return null;

        var raw = tenFileAudio.Trim();
        if (Uri.TryCreate(raw, UriKind.Absolute, out var absoluteUri) &&
            (absoluteUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             absoluteUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return absoluteUri.ToString();
        }

        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedBaseUrl) ||
            !Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out _))
        {
            return null;
        }

        return BuildAbsoluteUrl(normalizedBaseUrl, $"audio/{raw.TrimStart('/')}");
    }

    public static string BuildPoiApiUrl(string baseUrl)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        return string.IsNullOrWhiteSpace(normalizedBaseUrl) ||
               !Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out _)
            ? string.Empty
            : BuildAbsoluteUrl(normalizedBaseUrl, PoiApiPath);
    }

    public static string GetDefaultBaseUrl()
    {
        var normalizedBaseUrl = NormalizeBaseUrl(DefaultNgrokBaseUrl);
        return Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out _)
            ? normalizedBaseUrl
            : string.Empty;
    }

    private static string AppendSchemeIfMissing(string url)
    {
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var hostPart = url.Split(new[] { '/', '?', '#' }, 2)[0];
        return ShouldDefaultToHttps(hostPart)
            ? $"https://{url}"
            : $"http://{url}";
    }

    private static bool ShouldDefaultToHttps(string hostPart)
    {
        if (string.IsNullOrWhiteSpace(hostPart))
            return false;

        var host = hostPart;
        var colonIndex = host.LastIndexOf(':');
        if (colonIndex > 0 && host.IndexOf(':') == colonIndex)
            host = host[..colonIndex];

        host = host.Trim('[', ']');

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !IPAddress.TryParse(host, out _);
    }

    private static bool IsSupportedQrApiPath(string path)
    {
        var normalized = path.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized == "/")
            return true;

        normalized = normalized.TrimEnd('/');
        return normalized.Equals("/api", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals($"/{PoiApiPath}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeBinaryDownload(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".aab", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeBasePath(string path)
    {
        var normalized = path.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized) || normalized == "/")
            return string.Empty;

        normalized = normalized.StartsWith('/')
            ? normalized
            : $"/{normalized}";

        if (normalized.Equals("/api", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals($"/{PoiApiPath}", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return normalized;
    }

    private static string? GetLegacyConfiguredBaseUrl()
    {
        var customBaseUrl = Preferences.Get(ApiBaseUrlPreferenceKey, string.Empty)?.Trim();
        if (string.IsNullOrWhiteSpace(customBaseUrl))
            return null;

        var normalizedBaseUrl = NormalizeBaseUrl(customBaseUrl);
        return Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out _)
            ? normalizedBaseUrl
            : null;
    }

    private static string BuildAbsoluteUrl(string baseUrl, string relativePath)
    {
        var baseUri = new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);
        return new Uri(baseUri, relativePath.TrimStart('/')).ToString().TrimEnd('/');
    }
}
