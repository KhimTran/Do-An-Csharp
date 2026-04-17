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
            await DamBaoCotMoiPoiAsync();
            await _db.CreateTableAsync<LichSuPhatModel>();
            await _db.CreateTableAsync<AppSettingModel>();

            if (await _db.Table<PoiModel>().CountAsync() == 0)
                await ThemDuLieuMau();
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
            await _db!.InsertAllAsync(new List<PoiModel>
            {
                new PoiModel {
                    Ten = "Ốc Oanh",
                    MoTa_Vi = "Quán ốc nổi tiếng lâu năm trên phố ẩm thực Vĩnh Khánh.",
                    MoTa_En = "A long-standing famous seafood/snail restaurant on Vinh Khanh food street.",
                    MoTa_Zh = "位于永庆美食街的知名老牌海鲜店。",
                    Lat = 10.76147422883112,
                    Lng = 106.70258525764435,
                    BanKinh = 50,
                    UuTien = 1
                },
                new PoiModel {
                    Ten = "Ốc Thảo",
                    MoTa_Vi = "Quán ốc đông khách trên đường Vĩnh Khánh.",
                    MoTa_En = "A popular seafood spot on Vinh Khanh street.",
                    MoTa_Zh = "永庆街上的人气海鲜店。",
                    Lat = 10.760980,
                    Lng = 106.703420,
                    BanKinh = 65,
                    UuTien = 2
                },
                new PoiModel {
                    Ten = "Ốc Vũ",
                    MoTa_Vi = "Quán ốc mở khuya trong khu ẩm thực Vĩnh Khánh.",
                    MoTa_En = "A late-night seafood place in Vinh Khanh food area.",
                    MoTa_Zh = "永庆美食区内营业到深夜的海鲜店。",
                    Lat = 10.760760,
                    Lng = 106.703680,
                    BanKinh = 65,
                    UuTien = 3
                },
                new PoiModel {
                    Ten = "Ốc Phát",
                    MoTa_Vi = "Quán ốc quen thuộc với thực khách địa phương.",
                    MoTa_En = "A familiar seafood eatery for locals.",
                    MoTa_Zh = "本地食客常去的海鲜店。",
                    Lat = 10.760420,
                    Lng = 106.704080,
                    BanKinh = 65,
                    UuTien = 4
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
            return await _db!.InsertOrReplaceAsync(poi);
        }

        public async Task XoaPoiAsync(int id)
        {
            await KhoiTaoAsync();
            await _db!.DeleteAsync<PoiModel>(id);
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
