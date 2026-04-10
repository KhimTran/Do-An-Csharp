using App.Models;
using SQLite;

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
            await _db.CreateTableAsync<LichSuPhatModel>();
            await _db.CreateTableAsync<AppSettingModel>();
            if (await _db.Table<PoiModel>().CountAsync() == 0)
                await ThemDuLieuMau();
        }

        private async Task ThemDuLieuMau()
        {
            await _db!.InsertAllAsync(new List<PoiModel>
            {
                new PoiModel {
                    Ten = "Bún Bò Vĩnh Khánh",
                    MoTa_Vi = "Quán bún bò nổi tiếng lâu đời.",
                    MoTa_En = "Famous bun bo restaurant.",
                    Lat = 10.7565, Lng = 106.6896
                },
                new PoiModel {
                    Ten = "Chợ Xóm Chiếu",
                    MoTa_Vi = "Khu chợ truyền thống lâu đời.",
                    MoTa_En = "Traditional market.",
                    Lat = 10.7580, Lng = 106.6910
                }
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
            // Dữ liệu từ server thường có Id cố định (>0).
            // Nếu dùng UpdateAsync trực tiếp cho Id mới (chưa có trong SQLite) thì sẽ update 0 dòng.
            // InsertOrReplaceAsync giúp xử lý đúng cả 2 trường hợp:
            // - POI mới từ server -> INSERT
            // - POI đã tồn tại -> REPLACE/UPDATE
            return await _db!.InsertOrReplaceAsync(poi);
        }

        public async Task GhiLichSuPhatAsync(LichSuPhatModel lichSu)
        {
            await KhoiTaoAsync();
            await _db!.InsertAsync(lichSu);
        }

        public async Task<bool> KiemTraCooldownAsync(int poiId)
        {
            await KhoiTaoAsync();
            var lanCuoi = await _db!.Table<LichSuPhatModel>()
                .Where(l => l.PoiId == poiId && l.NguonKichHoat == "GPS")
                .OrderByDescending(l => l.ThoiGianPhat)
                .FirstOrDefaultAsync();

            if (lanCuoi == null) return true;
            return (DateTime.Now - lanCuoi.ThoiGianPhat).TotalMinutes >= 5;
        }


        public async Task<string?> LayCaiDatAsync(string key)
        {
            await KhoiTaoAsync();
            var setting = await _db!.Table<AppSettingModel>()
                .Where(x => x.Key == key)
                .FirstOrDefaultAsync();
            return setting?.Value;
        }

        public async Task LuuCaiDatAsync(string key, string value)
        {
            await KhoiTaoAsync();
            var setting = await _db!.Table<AppSettingModel>()
                .Where(x => x.Key == key)
                .FirstOrDefaultAsync();

            if (setting == null)
            {
                await _db.InsertAsync(new AppSettingModel { Key = key, Value = value });
                return;
            }

            setting.Value = value;
            await _db.UpdateAsync(setting);
        }

        public async Task<List<LichSuPhatModel>> LayLichSuMoiNhatAsync(int soLuong = 30)
        {
            await KhoiTaoAsync();
            return await _db!.Table<LichSuPhatModel>()
                .OrderByDescending(l => l.ThoiGianPhat)
                .Take(soLuong)
                .ToListAsync();
        }

        public async Task<List<ThongKePoiModel>> LayTopPoiDuocNgheNhieuAsync(int top = 5)
        {
            await KhoiTaoAsync();
            var lichSu = await _db!.Table<LichSuPhatModel>().ToListAsync();

            return lichSu
                .GroupBy(x => x.TenPoi)
                .Select(g => new ThongKePoiModel
                {
                    TenPoi = g.Key,
                    SoLanNghe = g.Count()
                })
                .OrderByDescending(x => x.SoLanNghe)
                .Take(top)
                .ToList();
        }
    }
}
