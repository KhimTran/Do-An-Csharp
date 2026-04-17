using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using App.Models;

namespace App.Services
{
    public class SyncService
    {
        private readonly LocalDatabase _db;
        private readonly HttpClient _http;

        // Ẩn endpoint khỏi giao diện người dùng cuối.
        // App sẽ tự thử danh sách endpoint này theo thứ tự.
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

                foreach (var url in DefaultApiUrls.Select(ChuanHoaApiUrl).Distinct())
                {
                    try
                    {
                        var danhSach = await _http.GetFromJsonAsync<List<PoiModel>>(url);
                        if (danhSach == null)
                            continue;

                        await _db.ThayTheTatCaPoiAsync(danhSach);

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
