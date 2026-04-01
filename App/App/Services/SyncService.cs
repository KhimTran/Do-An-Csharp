using System.Net.Http.Json;
using App.Models;

namespace App.Services
{
    public class SyncService
    {
        private readonly LocalDatabase _db;
        private readonly HttpClient _http;

        // ⚠️ Thay URL này bằng địa chỉ API đang chạy
        // Khi test trên emulator: dùng 10.0.2.2 thay cho localhost
        private const string API_URL = "http://10.0.2.2:5099/api/pois";

        public SyncService(LocalDatabase db)
        {
            _db = db;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        // Gọi hàm này khi app khởi động để đồng bộ POI từ server
        public async Task<bool> DongBoPoisAsync()
        {
            try
            {
                // Kiểm tra có mạng không trước khi gọi API
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                    return false;

                var danhSach = await _http.GetFromJsonAsync<List<PoiModel>>(API_URL);
                if (danhSach == null || danhSach.Count == 0)
                    return false;

                // Lưu từng POI vào SQLite local
                foreach (var poi in danhSach)
                    await _db.LuuPoiAsync(poi);

                return true;
            }
            catch
            {
                // Không có mạng hoặc server lỗi → dùng dữ liệu offline SQLite
                return false;
            }
        }
    }
}