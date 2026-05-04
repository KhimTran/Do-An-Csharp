using App.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public sealed record ApiBaseUrlUpdateResult(
        bool IsValid,
        string BaseUrl,
        string PoiApiUrl,
        bool SyncedFromServer,
        int LocalPoiCount,
        string Message);

    public class SyncService
    {
        private const string OfflineFallbackMessage = "Using local data (offline mode)";

        private readonly LocalDatabase _db;
        private readonly HttpClient _http;
        private string? _runtimeApiBaseUrlOverride;

        public string LastError { get; private set; } = string.Empty;

        public SyncService(LocalDatabase db)
        {
            _db = db;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task EnsureSavedApiConfigurationLoadedAsync()
        {
            var apiBaseUrlText = await _db.LayCaiDatAsync("api_base_url");
            if (!string.IsNullOrWhiteSpace(apiBaseUrlText))
                Preferences.Set("api_base_url", apiBaseUrlText.Trim());

            var offlineModeText = await _db.LayCaiDatAsync("offline_mode");
            if (bool.TryParse(offlineModeText, out var offlineMode))
                Preferences.Set("offline_mode", offlineMode);
        }

        public async Task<ApiBaseUrlUpdateResult> CapNhatApiBaseUrlTuQrAsync(string scannedUrl)
        {
            try
            {
                if (!ApiEndpointResolver.TryNormalizeApiBaseUrl(scannedUrl, out var baseUrl, out var poiApiUrl, out var errorMessage))
                {
                    return new ApiBaseUrlUpdateResult(
                        false,
                        string.Empty,
                        string.Empty,
                        false,
                        await DemSoPoiLocalAsync(),
                        errorMessage);
                }

                _runtimeApiBaseUrlOverride = baseUrl;
                Preferences.Set("api_base_url", baseUrl);
                Preferences.Set("offline_mode", false);

                await _db.LuuCaiDatAsync("api_base_url", baseUrl);
                await _db.LuuCaiDatAsync("offline_mode", false.ToString());

                bool synced = await DongBoPoisAsync();
                int localPoiCount = await DemSoPoiLocalAsync();
                string message = synced
                    ? $"Da nhan server URL tu QR: {baseUrl}. Da tai {localPoiCount} POI tu server."
                    : $"Da nhan server URL tu QR: {baseUrl}. App dang dung du lieu local ({localPoiCount} POI). {LastError}";

                return new ApiBaseUrlUpdateResult(
                    true,
                    baseUrl,
                    poiApiUrl,
                    synced,
                    localPoiCount,
                    message);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return new ApiBaseUrlUpdateResult(
                    false,
                    string.Empty,
                    string.Empty,
                    false,
                    await DemSoPoiLocalAsync(),
                    ex.Message);
            }
        }

        public async Task<bool> DongBoPoisAsync()
        {
            try
            {
                LastError = string.Empty;

                if (Preferences.Get("offline_mode", false))
                {
                    LastError = $"{OfflineFallbackMessage}. Offline mode is enabled.";
                    return false;
                }

                var danhSachMayChu = LayDanhSachMayChuDongBo().ToList();
                if (danhSachMayChu.Count == 0)
                {
                    LastError = $"{OfflineFallbackMessage}. API base URL is not configured.";
                    return false;
                }

                foreach (var mayChu in danhSachMayChu)
                {
                    try
                    {
                        var danhSach = await TaiDanhSachPoiTuServerAsync(mayChu.PoiApiUrl);
                        if (danhSach == null || danhSach.Count == 0)
                        {
                            LastError = $"{OfflineFallbackMessage}. API returned no POIs from {mayChu.PoiApiUrl}.";
                            continue;
                        }

                        foreach (var poi in danhSach)
                        {
                            poi.TenFileAnhMinhHoa = ChuanHoaUrlAnh(poi.TenFileAnhMinhHoa, mayChu.BaseUrl);
                            poi.TenFileAudio_Vi = ChuanHoaTenFileAudio(poi.TenFileAudio_Vi);
                            poi.TenFileAudio_En = ChuanHoaTenFileAudio(poi.TenFileAudio_En);
                            poi.TenFileAudio_Zh = ChuanHoaTenFileAudio(poi.TenFileAudio_Zh);
                            await GiuCacheAudioLocalNeuConHopLeAsync(poi);
                            await _db.LuuPoiAsync(poi);
                        }

                        var serverIds = danhSach.Select(p => p.Id).ToList();
                        await _db.XoaNhungPoiKhongConTrenServerAsync(serverIds);

                        return true;
                    }
                    catch (TaskCanceledException ex)
                    {
                        LastError = $"{OfflineFallbackMessage}. {mayChu.PoiApiUrl} -> timeout: {ex.Message}";
                    }
                    catch (HttpRequestException ex)
                    {
                        LastError = $"{OfflineFallbackMessage}. {mayChu.PoiApiUrl} -> network: {ex.Message}";
                    }
                    catch (JsonException ex)
                    {
                        LastError = $"{OfflineFallbackMessage}. {mayChu.PoiApiUrl} -> json: {ex.Message}";
                    }
                    catch (Exception ex)
                    {
                        LastError = $"{OfflineFallbackMessage}. {mayChu.PoiApiUrl} -> {ex.Message}";
                    }
                }

                if (string.IsNullOrWhiteSpace(LastError))
                    LastError = $"{OfflineFallbackMessage}. Could not load POIs from API.";

                return false;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        private IEnumerable<(string BaseUrl, string PoiApiUrl)> LayDanhSachMayChuDongBo()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (ApiEndpointResolver.TryNormalizeApiBaseUrl(_runtimeApiBaseUrlOverride, out var overrideBaseUrl, out var overridePoiApiUrl, out _)
                && seen.Add(overridePoiApiUrl))
            {
                yield return (overrideBaseUrl, overridePoiApiUrl);
            }

            foreach (var baseUrl in ApiEndpointResolver.GetBaseUrls())
            {
                var poiApiUrl = ApiEndpointResolver.BuildPoiApiUrl(baseUrl);
                if (!string.IsNullOrWhiteSpace(poiApiUrl) && seen.Add(poiApiUrl))
                    yield return (baseUrl, poiApiUrl);
            }
        }

        private static string? ChuanHoaUrlAnh(string? tenFileAnh, string baseUrl) =>
            ApiEndpointResolver.BuildPoiImageUrl(baseUrl, tenFileAnh);

        private static string? ChuanHoaTenFileAudio(string? tenFileAudio)
        {
            var normalized = tenFileAudio?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private async Task GiuCacheAudioLocalNeuConHopLeAsync(PoiModel poiMoi)
        {
            var poiLocal = await _db.LayPoiTheoIdAsync(poiMoi.Id);
            if (poiLocal == null)
                return;

            poiMoi.LocalAudioPath_Vi = LayDuongDanAudioLocalConHopLe(
                poiLocal.LocalAudioPath_Vi,
                poiLocal.TenFileAudio_Vi,
                poiMoi.TenFileAudio_Vi);
            poiMoi.LocalAudioPath_En = LayDuongDanAudioLocalConHopLe(
                poiLocal.LocalAudioPath_En,
                poiLocal.TenFileAudio_En,
                poiMoi.TenFileAudio_En);
            poiMoi.LocalAudioPath_Zh = LayDuongDanAudioLocalConHopLe(
                poiLocal.LocalAudioPath_Zh,
                poiLocal.TenFileAudio_Zh,
                poiMoi.TenFileAudio_Zh);

            poiMoi.LocalAudioCachedAt =
                !string.IsNullOrWhiteSpace(poiMoi.LocalAudioPath_Vi) ||
                !string.IsNullOrWhiteSpace(poiMoi.LocalAudioPath_En) ||
                !string.IsNullOrWhiteSpace(poiMoi.LocalAudioPath_Zh)
                    ? poiLocal.LocalAudioCachedAt
                    : null;
        }

        private static string? LayDuongDanAudioLocalConHopLe(
            string? localPath,
            string? tenFileAudioCu,
            string? tenFileAudioMoi)
        {
            if (string.IsNullOrWhiteSpace(localPath))
                return null;

            if (!string.Equals(tenFileAudioCu?.Trim(), tenFileAudioMoi?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                XoaFileCacheCuNeuCo(localPath);
                return null;
            }

            return File.Exists(localPath) ? localPath : null;
        }

        private static void XoaFileCacheCuNeuCo(string localPath)
        {
            try
            {
                if (File.Exists(localPath))
                    File.Delete(localPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SyncService] Khong xoa duoc audio cache cu {localPath}: {ex.Message}");
            }
        }

        private async Task<List<PoiModel>?> TaiDanhSachPoiTuServerAsync(string poiApiUrl)
        {
            var cacheBustUrl = $"{poiApiUrl}{(poiApiUrl.Contains('?') ? "&" : "?")}ts={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            using var request = new HttpRequestMessage(HttpMethod.Get, cacheBustUrl);
            request.Headers.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MaxAge = TimeSpan.Zero
            };
            request.Headers.Pragma.ParseAdd("no-cache");

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<PoiModel>>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private async Task<int> DemSoPoiLocalAsync() =>
            (await _db.LayTatCaPoiAsync()).Count;
    }
}
