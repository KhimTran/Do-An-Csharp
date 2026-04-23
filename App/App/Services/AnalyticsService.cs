using System.Net.Http.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class AnalyticsService
    {
        private static readonly TimeSpan KhoangCachGuiRoutePing = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan KhoangCachGuiHeartbeat = TimeSpan.FromSeconds(60);
        private readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        private DateTime _lanGuiRoutePingCuoi = DateTime.MinValue;
        private DateTime _lanGuiHeartbeatCuoi = DateTime.MinValue;

        public string LastError { get; private set; } = string.Empty;

        private static IEnumerable<string> LayDanhSachBaseUrlCanThu()
        {
            foreach (var url in ApiEndpointResolver.GetBaseUrls())
                yield return url;
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
                    LastError = "Offline mode dang bat.";
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

                string sessionId = LayHoacTaoSessionId();

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
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task GuiHeartbeatAsync()
        {
            try
            {
                if (Preferences.Get("offline_mode", false))
                    return;

                var now = DateTime.UtcNow;
                if (now - _lanGuiHeartbeatCuoi < KhoangCachGuiHeartbeat)
                    return;

                _lanGuiHeartbeatCuoi = now;

                var payload = new
                {
                    SessionId = LayHoacTaoSessionId(),
                    DeviceLabel = LayNhanThietBi(),
                    AppVersion = AppInfo.VersionString
                };

                foreach (var baseUrl in LayDanhSachBaseUrlCanThu().Distinct())
                {
                    try
                    {
                        var response = await _http.PostAsJsonAsync(
                            $"{baseUrl}/api/analytics/heartbeat",
                            payload);

                        if (response.IsSuccessStatusCode)
                            return;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
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

        private static string LayHoacTaoSessionId()
        {
            string sessionId = Preferences.Get("analytics_session_id", string.Empty);
            if (!string.IsNullOrWhiteSpace(sessionId))
                return sessionId;

            sessionId = Guid.NewGuid().ToString("N");
            Preferences.Set("analytics_session_id", sessionId);
            return sessionId;
        }

        private static string LayNhanThietBi()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
                return "Android";

            if (DeviceInfo.Platform == DevicePlatform.iOS)
                return "iOS";

            return DeviceInfo.Platform.ToString();
        }
    }
}
