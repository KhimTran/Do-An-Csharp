using System.Net.Http.Json;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class AnalyticsService
    {
        private readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly string[] DefaultBaseUrls =
        {
            "http://10.0.2.2:5099",   // Android Emulator
            "http://localhost:5099"   // Windows/local
        };

        public string LastError { get; private set; } = string.Empty;

        private static string ChuanHoaBaseUrl(string url)
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
                return url.Replace("localhost", "10.0.2.2", StringComparison.OrdinalIgnoreCase);

            return url;
        }

        private static IEnumerable<string> LayDanhSachBaseUrlCanThu()
        {
            var customPoiUrl = Preferences.Get("api_poi_url", string.Empty);

            if (!string.IsNullOrWhiteSpace(customPoiUrl))
            {
                Uri? uri = null;

                try
                {
                    uri = new Uri(customPoiUrl.Trim());
                }
                catch
                {
                }

                if (uri != null)
                {
                    yield return ChuanHoaBaseUrl($"{uri.Scheme}://{uri.Authority}");
                }
            }

            foreach (var url in DefaultBaseUrls)
                yield return ChuanHoaBaseUrl(url);
        }

        public async Task<bool> GuiLogAsync(
            int poiId,
            string poiTen,
            string nguon,
            int thoiLuongGiay)
        {
            try
            {
                LastError = string.Empty;

                if (Preferences.Get("offline_mode", false))
                {
                    LastError = "Offline mode đang bật";
                    return false;
                }

                var payload = new
                {
                    PoiId = poiId,
                    PoiTen = poiTen,
                    Nguon = nguon,
                    ThoiLuongGiay = thoiLuongGiay
                };

                foreach (var baseUrl in LayDanhSachBaseUrlCanThu().Distinct())
                {
                    try
                    {
                        var response = await _http.PostAsJsonAsync(
                            $"{baseUrl}/api/analytics/logs",
                            payload);

                        if (response.IsSuccessStatusCode)
                            return true;

                        LastError = $"{baseUrl} -> HTTP {(int)response.StatusCode}";
                    }
                    catch (Exception ex)
                    {
                        LastError = $"{baseUrl} -> {ex.Message}";
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public static int UocTinhThoiLuongGiay(string noiDung)
        {
            if (string.IsNullOrWhiteSpace(noiDung))
                return 0;

            var soTu = noiDung
                .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Length;

            return Math.Max(5, soTu / 2);
        }
    }
}