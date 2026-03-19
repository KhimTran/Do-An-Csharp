using SQLite;
using App.Models;

namespace App.Services
{
    public class LocalDatabase
    {
        private SQLiteAsyncConnection? _db;

        public async Task KhoiTaoAsync()
        {
            if (_db != null) return;

            string duongDan = Path.Combine(
                FileSystem.AppDataDirectory, "vinhkhanh.db3"
            );

            _db = new SQLiteAsyncConnection(duongDan);
            await _db.CreateTableAsync<PoiModel>();

            if (await _db.Table<PoiModel>().CountAsync() == 0)
                await ThemDuLieuMau();
        }

        private async Task ThemDuLieuMau()
        {
            await _db!.InsertAllAsync(new List<PoiModel>
            {
                new PoiModel {
                    Ten = "Quán Bún Bò Huế Vĩnh Khánh",
                    MoTa_Vi = "Quán bún bò nổi tiếng với hơn 30 năm lịch sử.",
                    MoTa_En = "Famous bun bo Hue restaurant with 30-year history.",
                    Lat = 10.7565, Lng = 106.6896, BanKinh = 50, UuTien = 1
                },
                new PoiModel {
                    Ten = "Chợ Xóm Chiếu",
                    MoTa_Vi = "Khu chợ truyền thống lâu đời của quận 4.",
                    MoTa_En = "Traditional market of District 4.",
                    Lat = 10.7580, Lng = 106.6910, BanKinh = 80, UuTien = 2
                },
            });
        }

        public async Task<List<PoiModel>> LayTatCaPoiAsync()
        {
            await KhoiTaoAsync();
            return await _db!.Table<PoiModel>()
                              .OrderBy(p => p.UuTien)
                              .ToListAsync();
        }

        public async Task<PoiModel?> LayPoiTheoIdAsync(int id)
        {
            await KhoiTaoAsync();
            return await _db!.Table<PoiModel>()
                              .Where(p => p.Id == id)
                              .FirstOrDefaultAsync();
        }

        public async Task<int> LuuPoiAsync(PoiModel poi)
        {
            await KhoiTaoAsync();
            return poi.Id == 0
                ? await _db!.InsertAsync(poi)
                : await _db!.UpdateAsync(poi);
        }
    }
}