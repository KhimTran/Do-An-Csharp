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
                    Ten = "Quán Bún Bò Huế Vĩnh Khánh",
                    MoTa_Vi = "Quán bún bò nổi tiếng với hơn 30 năm lịch sử.",
                    MoTa_En = "Famous bun bo Hue restaurant with 30-year history.",
                    MoTa_Zh = "著名的顺化牛肉粉餐厅，拥有30多年历史。",
                    Lat = 10.7565, Lng = 106.6896, BanKinh = 50, UuTien = 1
                },
                new PoiModel {
                    Ten = "Chợ Xóm Chiếu",
                    MoTa_Vi = "Khu chợ truyền thống lâu đời của quận 4.",
                    MoTa_En = "Traditional market of District 4.",
                    MoTa_Zh = "第四郡历史悠久的传统市场。",
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

        // Ghi lại lịch sử phát âm (dùng cho Cooldown và Analytics)
        public async Task GhiLichSuPhatAsync(LichSuPhatModel lichSu)
        {
            await KhoiTaoAsync();
            await _db!.InsertAsync(lichSu);
        }

        // Kiểm tra cooldown: trả về true nếu được phép phát (chưa phát trong 5 phút)
        public async Task<bool> KiemTraCooldownAsync(int poiId)
        {
            await KhoiTaoAsync();
            var lanCuoi = await _db!.Table<LichSuPhatModel>()
                .Where(l => l.PoiId == poiId && l.NguonKichHoat == "GPS")
                .OrderByDescending(l => l.ThoiGianPhat)
                .FirstOrDefaultAsync();

            if (lanCuoi == null) return true; // Chưa từng phát → cho phép
            return (DateTime.Now - lanCuoi.ThoiGianPhat).TotalMinutes >= 5;
        }

        // Analytics: đếm số lần nghe theo từng POI để làm báo cáo
        public async Task<List<ThongKePoiNgheModel>> LayThongKeSoLanNgheTheoPoiAsync()
        {
            await KhoiTaoAsync();
            const string sql = @"
                SELECT
                    PoiId,
                    TenPoi,
                    COUNT(*) AS SoLanNghe,
                    MAX(ThoiGianPhat) AS LanNgheGanNhat
                FROM LichSuPhat
                GROUP BY PoiId, TenPoi
                ORDER BY SoLanNghe DESC, LanNgheGanNhat DESC";

            return await _db!.QueryAsync<ThongKePoiNgheModel>(sql);
        }

        // Analytics tổng quan: tổng số lượt nghe toàn hệ thống
        public async Task<int> DemTongLuotNgheAsync()
        {
            await KhoiTaoAsync();
            return await _db!.Table<LichSuPhatModel>().CountAsync();
        }
    }
}
