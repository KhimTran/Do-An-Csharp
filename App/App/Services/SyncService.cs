using System.Net.Http.Json;
using App.Models;
using Microsoft.Maui.Storage;

namespace App.Services
{
    public class SyncService
    {
        private readonly LocalDatabase _db;
        private readonly HttpClient _http;

        private static readonly string[] DefaultApiUrls =
        {
            "http://10.0.2.2:5099/api/pois", // Android Emulator -> host machine
            "http://10.0.2.2:7074/api/pois", // fallback profile
            "http://localhost:5099/api/pois" // local desktop testing
        };

        public string LastError { get; private set; } = string.Empty;

        public SyncService(LocalDatabase db)
        {
            _db = db;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }


        private static string ChuanHoaApiUrl(string url)
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
                return url.Replace("localhost", "10.0.2.2", StringComparison.OrdinalIgnoreCase);

            return url;
        }

        private static IEnumerable<string> LayDanhSachApiCanThu(string? customUrl)
        {
            if (!string.IsNullOrWhiteSpace(customUrl))
                yield return ChuanHoaApiUrl(customUrl.Trim());

            foreach (var url in DefaultApiUrls)
                yield return ChuanHoaApiUrl(url);
        }

        // Gọi hàm này khi app khởi động để đồng bộ POI từ server
        public async Task<bool> DongBoPoisAsync()
        {
            try
            {
                LastError = string.Empty;

                // Nếu bật offline mode thì không gọi API
                bool offlineMode = Preferences.Get("offline_mode", false);
                if (offlineMode)
                {
                    LastError = "Offline mode đang bật";
                    return false;
                }

                var customUrl = Preferences.Get("api_poi_url", string.Empty);

                foreach (var url in LayDanhSachApiCanThu(customUrl).Distinct())
                {
                    try
                    {
                        var danhSach = await _http.GetFromJsonAsync<List<PoiModel>>(url);
                        if (danhSach == null || danhSach.Count == 0)
                            continue;

                        foreach (var poi in danhSach)
                            await _db.LuuPoiAsync(poi);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        LastError = $"{url} -> {ex.Message}";
                    }
                }

                if (string.IsNullOrWhiteSpace(LastError))
                    LastError = "Không lấy được dữ liệu từ API";

                return false;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }
    }
}
