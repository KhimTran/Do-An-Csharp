using App.Models;
using SQLite;

namespace App.Services
{
    public class LocalDatabase
    {
        private const string PoiCacheSchemaVersion = "2026-04-24-poi-text-tts-v1";
        private SQLiteAsyncConnection? _db;

        public async Task KhoiTaoAsync()
        {
            if (_db != null) return;

            string duongDan = Path.Combine(
                FileSystem.AppDataDirectory, "vinhkhanh.db3"
            );

            _db = new SQLiteAsyncConnection(duongDan);
            await _db.CreateTableAsync<PoiModel>();
            await DamBaoCotMoiPoiAsync();
            await _db.CreateTableAsync<LichSuPhatModel>();
            await _db.CreateTableAsync<AppSettingModel>();
            await DamBaoPhienBanCachePoiAsync();

            if (await _db.Table<PoiModel>().CountAsync() == 0)
                await ThemDuLieuMau();
        }

        private async Task DamBaoPhienBanCachePoiAsync()
        {
            var schemaVersion = await LayCaiDatNoiBoAsync("poi_cache_schema_version");
            if (string.Equals(schemaVersion, PoiCacheSchemaVersion, StringComparison.Ordinal))
                return;

            await _db!.DeleteAllAsync<PoiModel>();
            await LuuCaiDatNoiBoAsync("poi_cache_schema_version", PoiCacheSchemaVersion);
        }

        private async Task DamBaoCotMoiPoiAsync()
        {
            // SQLite-net không tự ALTER khi model đổi, nên chủ động thêm cột để tương thích phiên bản app mới.
            string[] lenhAlter =
            {
                "ALTER TABLE POIs ADD COLUMN SoDienThoai TEXT",
                "ALTER TABLE POIs ADD COLUMN GioMoCua TEXT",
                "ALTER TABLE POIs ADD COLUMN GioDongCua TEXT",
                "ALTER TABLE POIs ADD COLUMN MonDacTrung TEXT",
                "ALTER TABLE POIs ADD COLUMN GalleryJson TEXT",
                "ALTER TABLE POIs ADD COLUMN TenFileAnhMinhHoa TEXT",
                "ALTER TABLE POIs ADD COLUMN TenFileAudio_Vi TEXT",
                "ALTER TABLE POIs ADD COLUMN TenFileAudio_En TEXT",
                "ALTER TABLE POIs ADD COLUMN TenFileAudio_Zh TEXT",
                "ALTER TABLE POIs ADD COLUMN LocalAudioPath_Vi TEXT",
                "ALTER TABLE POIs ADD COLUMN LocalAudioPath_En TEXT",
                "ALTER TABLE POIs ADD COLUMN LocalAudioPath_Zh TEXT",
                "ALTER TABLE POIs ADD COLUMN LocalAudioCachedAt TEXT",
                "ALTER TABLE POIs ADD COLUMN QrCodeNoiDung TEXT",
                "ALTER TABLE POIs ADD COLUMN TtsVoiceCode TEXT",
                "ALTER TABLE POIs ADD COLUMN TrangThaiDuyet TEXT",
                "ALTER TABLE POIs ADD COLUMN NgayDuyet TEXT"
            };

            foreach (var sql in lenhAlter)
            {
                try
                {
                    await _db!.ExecuteAsync(sql);
                }
                catch
                {
                    // Cột đã tồn tại thì bỏ qua.
                }
            }
        }

        private async Task ThemDuLieuMau()
        {
            var mau = new List<PoiModel>
            {
                new PoiModel
                {
                    Id = 1,
                    Ten = "Pho am thuc Vinh Khanh",
                    MoTa_Vi = "Cum quan an noi tieng tren duong Vinh Khanh, phu hop de demo marker va geofence ngay khi mo app.",
                    MoTa_En = "A well-known food street cluster on Vinh Khanh, useful to demo markers and geofences right after launch.",
                    MoTa_Zh = "永庆街知名美食区，打开应用即可看到示例标记与地理围栏。",
                    Lat = 10.76082,
                    Lng = 106.70442,
                    BanKinh = 80,
                    UuTien = 1
                },
                new PoiModel
                {
                    Id = 2,
                    Ten = "Cho Xom Chieu",
                    MoTa_Vi = "Cho truyen thong lau doi o Quan 4, duoc seed san de app van co du lieu khi API khong san sang.",
                    MoTa_En = "A long-running traditional market in District 4, seeded so the app still has data when the API is unavailable.",
                    MoTa_Zh = "第四郡的传统市场，在 API 不可用时仍能用作本地示例数据。",
                    Lat = 10.75873,
                    Lng = 106.70169,
                    BanKinh = 100,
                    UuTien = 2
                }
            };

            foreach (var poi in mau)
                await _db!.InsertOrReplaceAsync(poi);
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
            return await _db!.InsertOrReplaceAsync(poi);
        }

        public async Task XoaPoiAsync(int id)
        {
            await KhoiTaoAsync();
            await _db!.DeleteAsync<PoiModel>(id);
        }

        public async Task XoaTatCaDuongDanAudioLocalAsync()
        {
            await KhoiTaoAsync();

            var danhSachPoi = await _db!.Table<PoiModel>().ToListAsync();
            foreach (var poi in danhSachPoi)
            {
                poi.LocalAudioPath_Vi = null;
                poi.LocalAudioPath_En = null;
                poi.LocalAudioPath_Zh = null;
                poi.LocalAudioCachedAt = null;
                await _db.UpdateAsync(poi);
            }
        }

        public async Task XoaNhungPoiKhongConTrenServerAsync(List<int> serverIds)
        {
            await KhoiTaoAsync();

            var tatCaPoiLocal = await _db!.Table<PoiModel>().ToListAsync();

            var idsCanXoa = tatCaPoiLocal
                .Where(p => !serverIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToList();

            foreach (var id in idsCanXoa)
                await _db.DeleteAsync<PoiModel>(id);
        }

        public async Task GhiLichSuPhatAsync(LichSuPhatModel lichSu)
        {
            await KhoiTaoAsync();
            await _db!.InsertAsync(lichSu);
        }

        public async Task<bool> KiemTraCooldownAsync(int poiId)
        {
            return await KiemTraCooldownAsync(poiId, TimeSpan.FromMinutes(5));
        }

        public async Task<bool> KiemTraCooldownAsync(int poiId, TimeSpan cooldown)
        {
            await KhoiTaoAsync();

            var lanCuoi = await LayLanPhatGpsGanNhatAsync(poiId);
            if (!lanCuoi.HasValue) return true;

            return DateTime.Now - lanCuoi.Value >= cooldown;
        }

        public async Task<DateTime?> LayLanPhatGpsGanNhatAsync(int poiId)
        {
            await KhoiTaoAsync();

            var lanCuoi = await _db!.Table<LichSuPhatModel>()
                .Where(l => l.PoiId == poiId && l.NguonKichHoat == "GPS")
                .OrderByDescending(l => l.ThoiGianPhat)
                .FirstOrDefaultAsync();

            return lanCuoi?.ThoiGianPhat;
        }

        public async Task<string?> LayCaiDatAsync(string key)
        {
            await KhoiTaoAsync();

            return await LayCaiDatNoiBoAsync(key);
        }

        public async Task LuuCaiDatAsync(string key, string value)
        {
            await KhoiTaoAsync();
            await LuuCaiDatNoiBoAsync(key, value);
        }

        private async Task<string?> LayCaiDatNoiBoAsync(string key)
        {
            var setting = await _db!.Table<AppSettingModel>()
                .Where(x => x.Key == key)
                .FirstOrDefaultAsync();

            return setting?.Value;
        }

        private async Task LuuCaiDatNoiBoAsync(string key, string value)
        {
            var setting = await _db!.Table<AppSettingModel>()
                .Where(x => x.Key == key)
                .FirstOrDefaultAsync();

            if (setting == null)
            {
                await _db.InsertAsync(new AppSettingModel
                {
                    Key = key,
                    Value = value
                });
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
