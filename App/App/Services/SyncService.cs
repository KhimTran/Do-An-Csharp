using App.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;

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

                if (Preferences.Get("offline_mode", false))
                {
                    LastError = "Offline mode dang bat.";
                    return false;
                }

                var danhSachUrl = ApiEndpointResolver.GetPoiApiUrls().Distinct().ToList();
                if (danhSachUrl.Count == 0)
                {
                    LastError = "Chua cau hinh API base URL. Dang dung du lieu SQLite/local sample.";
                    return false;
                }

                foreach (var url in danhSachUrl)
                {
                    try
                    {
                        var danhSach = await _http.GetFromJsonAsync<List<PoiModel>>(url);
                        if (danhSach == null || danhSach.Count == 0)
                            continue;

                        foreach (var poi in danhSach)
                        {
                            poi.TenFileAnhMinhHoa = ChuanHoaUrlAnh(poi.TenFileAnhMinhHoa);
                            await _db.LuuPoiAsync(poi);
                        }

                        var serverIds = danhSach.Select(p => p.Id).ToList();
                        await _db.XoaNhungPoiKhongConTrenServerAsync(serverIds);

                        return true;
                    }
                    catch (TaskCanceledException ex)
                    {
                        LastError = $"{url} -> timeout: {ex.Message}";
                    }
                    catch (HttpRequestException ex)
                    {
                        LastError = $"{url} -> network: {ex.Message}";
                    }
                    catch (JsonException ex)
                    {
                        LastError = $"{url} -> json: {ex.Message}";
                    }
                    catch (Exception ex)
                    {
                        LastError = $"{url} -> {ex.Message}";
                    }
                }

                if (string.IsNullOrWhiteSpace(LastError))
                    LastError = "Khong lay duoc du lieu tu API.";

                return false;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        private static string? ChuanHoaUrlAnh(string? tenFileAnh) =>
            ApiEndpointResolver.BuildPoiImageUrl(tenFileAnh);
    }
}
