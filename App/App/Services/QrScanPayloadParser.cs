using System.Net;
using System.Text.Json;

namespace App.Services;

public sealed class QrScanPayload
{
    public string RawValue { get; init; } = string.Empty;
    public int? PoiId { get; init; }
    public string? ApkUrl { get; init; }
    public string? ApiBaseUrlCandidate { get; init; }
}

public static class QrScanPayloadParser
{
    private static readonly string[] ApkKeys =
    [
        "apk",
        "apkurl",
        "apk_url",
        "apklink",
        "apk_link",
        "download",
        "downloadurl",
        "download_url"
    ];

    private static readonly string[] ApiKeys =
    [
        "api",
        "apiurl",
        "api_url",
        "baseurl",
        "base_url",
        "server",
        "serverurl",
        "server_url",
        "ngrok",
        "endpoint",
        "poiapi",
        "poi_api"
    ];

    public static QrScanPayload Parse(string? rawQr)
    {
        var raw = rawQr?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return new QrScanPayload();

        if (TryExtractPoiId(raw, out var poiId))
        {
            return new QrScanPayload
            {
                RawValue = raw,
                PoiId = poiId
            };
        }

        var namedValues = ReadNamedValues(raw);
        var apiCandidate = TryPickUrl(namedValues, ApiKeys, LooksLikeHttpUrl);
        var apkUrl = TryPickUrl(namedValues, ApkKeys, IsApkDownloadUrl);

        if (!string.IsNullOrWhiteSpace(apiCandidate) || !string.IsNullOrWhiteSpace(apkUrl))
        {
            return new QrScanPayload
            {
                RawValue = raw,
                ApiBaseUrlCandidate = apiCandidate,
                ApkUrl = apkUrl
            };
        }

        if (IsApkDownloadUrl(raw))
        {
            return new QrScanPayload
            {
                RawValue = raw,
                ApkUrl = EnsureAbsoluteHttpUrl(raw)
            };
        }

        if (LooksLikeHttpUrl(raw))
        {
            return new QrScanPayload
            {
                RawValue = raw,
                ApiBaseUrlCandidate = EnsureAbsoluteHttpUrl(raw)
            };
        }

        return new QrScanPayload
        {
            RawValue = raw
        };
    }

    public static bool TryExtractPoiId(string raw, out int poiId)
    {
        poiId = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();

        if (int.TryParse(trimmed, out poiId))
            return true;

        const string poiPrefix = "poi:";
        if (trimmed.StartsWith(poiPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var poiIdText = trimmed[poiPrefix.Length..].Trim();
            return int.TryParse(poiIdText, out poiId);
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return false;

        return TryReadPoiIdFromQuery(uri.Query, out poiId);
    }

    private static Dictionary<string, string> ReadNamedValues(string raw)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        TryReadJsonObject(raw, values);
        TryReadUrlQuery(raw, values);
        TryReadDelimitedPairs(raw, values);

        return values;
    }

    private static void TryReadJsonObject(string raw, IDictionary<string, string> values)
    {
        if (!raw.StartsWith('{'))
            return;

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return;

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                    continue;

                var value = property.Value.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    values[property.Name] = value.Trim();
            }
        }
        catch
        {
            // QR khong o dang JSON thi bo qua.
        }
    }

    private static void TryReadUrlQuery(string raw, IDictionary<string, string> values)
    {
        if (!LooksLikeHttpUrl(raw))
            return;

        var candidate = EnsureAbsoluteHttpUrl(raw);
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Query))
        {
            return;
        }

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
                continue;

            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            if (!string.IsNullOrWhiteSpace(value))
                values[key] = value.Trim();
        }
    }

    private static void TryReadDelimitedPairs(string raw, IDictionary<string, string> values)
    {
        var segments = raw.Split(['\r', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
                separatorIndex = segment.IndexOf(':');

            if (separatorIndex <= 0)
                continue;

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                values[key] = value;
        }
    }

    private static string? TryPickUrl(
        IReadOnlyDictionary<string, string> values,
        IEnumerable<string> keys,
        Func<string, bool> validator)
    {
        foreach (var key in keys)
        {
            if (!values.TryGetValue(key, out var value) || !validator(value))
                continue;

            return EnsureAbsoluteHttpUrl(value);
        }

        return null;
    }

    private static bool TryReadPoiIdFromQuery(string query, out int poiId)
    {
        poiId = 0;
        if (string.IsNullOrWhiteSpace(query))
            return false;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            var key = Uri.UnescapeDataString(parts[0]);
            if (!key.Equals("poiId", StringComparison.OrdinalIgnoreCase) &&
                !key.Equals("poi", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = Uri.UnescapeDataString(parts[1]);
            if (int.TryParse(value, out poiId))
                return true;
        }

        return false;
    }

    private static bool LooksLikeHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (trimmed.Contains(' ') || trimmed.Contains('\r') || trimmed.Contains('\n'))
            return false;

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var hostPart = trimmed.Split(new[] { '/', '?', '#' }, 2)[0];
        if (hostPart.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (hostPart.Contains('.'))
            return true;

        return char.IsDigit(hostPart.FirstOrDefault()) && hostPart.Contains(':');
    }

    private static bool IsApkDownloadUrl(string? value)
    {
        if (!LooksLikeHttpUrl(value))
            return false;

        var candidate = EnsureAbsoluteHttpUrl(value!);
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return false;

        return uri.AbsolutePath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) ||
               uri.AbsolutePath.EndsWith(".aab", StringComparison.OrdinalIgnoreCase) ||
               uri.AbsolutePath.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureAbsoluteHttpUrl(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        var hostPart = trimmed.Split(new[] { '/', '?', '#' }, 2)[0];
        var hostOnly = hostPart;
        var colonIndex = hostOnly.LastIndexOf(':');
        if (colonIndex > 0 && hostOnly.IndexOf(':') == colonIndex)
            hostOnly = hostOnly[..colonIndex];

        hostOnly = hostOnly.Trim('[', ']');
        return hostOnly.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               IPAddress.TryParse(hostOnly, out _)
            ? $"http://{trimmed}"
            : $"https://{trimmed}";
    }
}
