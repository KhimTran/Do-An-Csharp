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
            "http://10.0.2.2:5099/api/pois",
            "http://localhost:5099/api/pois"
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

        public async Task<bool> DongBoPoisAsync()
        {
            try
            {
                LastError = string.Empty;

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
                        if (danhSach == null)
                            continue;

                        foreach (var poi in danhSach)
                            await _db.LuuPoiAsync(poi);

                        var serverIds = danhSach.Select(p => p.Id).ToList();
                        await _db.XoaNhungPoiKhongConTrenServerAsync(serverIds);

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