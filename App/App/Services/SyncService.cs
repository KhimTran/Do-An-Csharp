// Services/SyncService.cs
using System.Net.Http.Json;
using App.Models;

namespace App.Services
{
    public class SyncService
    {
        private readonly HttpClient _http;
        private readonly LocalDatabase _db;

        public SyncService(LocalDatabase db)
        {
            _db = db;
            // Lưu ý: Thay URL này bằng địa chỉ localhost hoặc server thật của bạn sau khi deploy
            _http = new HttpClient { BaseAddress = new Uri("https://your-api.azurewebsites.net/") };
        }

        public async Task DongBoAsync()
        {
            // Kiểm tra kết nối mạng, nếu không có internet thì thoát hàm, app tiếp tục dùng data offline
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return;

            try
            {
                // Gọi API lấy danh sách POI mới nhất từ Server
                var poiTuServer = await _http.GetFromJsonAsync<List<PoiModel>>("api/pois");

                if (poiTuServer == null || !poiTuServer.Any()) return;

                // Upsert dữ liệu vào SQLite local
                foreach (var poi in poiTuServer)
                {
                    await _db.LuuPoiAsync(poi);
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi để debug
                System.Diagnostics.Debug.WriteLine($"[SyncService] Lỗi đồng bộ: {ex.Message}");
            }
        }
    }
}