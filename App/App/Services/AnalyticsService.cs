using System.Net.Http.Json;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class AnalyticsService
    {
        private static readonly TimeSpan KhoangCachGuiRoutePing = TimeSpan.FromSeconds(15);
        private readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        private DateTime _lanGuiRoutePingCuoi = DateTime.MinValue;

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

        public async Task<bool> GuiRoutePingAsync(double lat, double lng, string nguon = "GPS")
        {
            try
            {
                if (Preferences.Get("offline_mode", false))
                    return false;

                if (DateTime.UtcNow - _lanGuiRoutePingCuoi < KhoangCachGuiRoutePing)
                    return false;

                string sessionId = Preferences.Get("analytics_session_id", string.Empty);
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    sessionId = Guid.NewGuid().ToString("N");
                    Preferences.Set("analytics_session_id", sessionId);
                }

                var payload = new[]
                {
                    new
                    {
                        SessionId = sessionId,
                        Lat = lat,
                        Lng = lng,
                        ThoiGian = DateTime.UtcNow,
                        Nguon = nguon
                    }
                };

                foreach (var baseUrl in LayDanhSachBaseUrlCanThu().Distinct())
                {
                    try
                    {
                        var response = await _http.PostAsJsonAsync(
                            $"{baseUrl}/api/analytics/route-pings",
                            payload);

                        if (response.IsSuccessStatusCode)
                        {
                            _lanGuiRoutePingCuoi = DateTime.UtcNow;
                            return true;
                        }
                    }
                    catch
                    {
                        // Không throw để tránh làm hỏng luồng GPS.
                    }
                }

                return false;
            }
            catch
            {
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
