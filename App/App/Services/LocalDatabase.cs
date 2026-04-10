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
            await _db.CreateTableAsync<LichSuPhatModel>();

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
            return poi.Id == 0
                ? await _db!.InsertAsync(poi)
                : await _db!.UpdateAsync(poi);
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

        // 🔥 LẤY LỊCH SỬ
        public async Task<List<LichSuPhatModel>> LayLichSuPhatAsync()
        {
            await KhoiTaoAsync();
            return await _db!.Table<LichSuPhatModel>()
                .OrderByDescending(x => x.ThoiGianPhat)
                .ToListAsync();
        }

        // 🔥 THỐNG KÊ
        public async Task<List<ThongKePoiModel>> LayThongKePoiAsync()
        {
            await KhoiTaoAsync();

            var lichSu = await _db!.Table<LichSuPhatModel>().ToListAsync();

            return lichSu
                .GroupBy(x => x.TenPoi)
                .Select(g => new ThongKePoiModel
                {
                    TenPoi = g.Key,
                    SoLanNghe = g.Count(),
                    LanNgheCuoi = g.Max(x => x.ThoiGianPhat)
                })
                .OrderByDescending(x => x.SoLanNghe)
                .ToList();
        }
    }
}