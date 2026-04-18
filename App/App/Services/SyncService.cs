using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using App.Models;

namespace App.Services
{
    public class SyncService
    {
        private readonly LocalDatabase _db;
        private readonly HttpClient _http;

        public string LastError { get; private set; } = string.Empty;

        public SyncService(LocalDatabase db)
        {
            _db = db;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
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

                foreach (var url in ApiEndpointResolver.GetPoiApiUrls().Distinct())
                {
                    try
                    {
                        var danhSach = await _http.GetFromJsonAsync<List<PoiModel>>(url);
                        if (danhSach == null)
                            continue;

                        foreach (var poi in danhSach)
                        {
                            poi.TenFileAnhMinhHoa = ChuanHoaUrlAnh(baseUrl: url, tenFileAnh: poi.TenFileAnhMinhHoa);
                        }

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

        private static string? ChuanHoaUrlAnh(string baseUrl, string? tenFileAnh)
        {
            if (string.IsNullOrWhiteSpace(tenFileAnh))
                return null;

            var raw = tenFileAnh.Trim();
            if (Uri.TryCreate(raw, UriKind.Absolute, out _))
                return raw;

            var normalizedBase = baseUrl.Replace("/api/pois", string.Empty, StringComparison.OrdinalIgnoreCase).TrimEnd('/');
            return $"{normalizedBase}/images/poi/{raw}";
        }
    }
}
