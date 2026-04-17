using Microsoft.EntityFrameworkCore;
using VinhKhanhApi.Models;

namespace VinhKhanhApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<PoiModel> POIs { get; set; }
        public DbSet<PlaybackLogModel> PlaybackLogs { get; set; }
        public DbSet<UserAccountModel> UserAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PoiModel>().HasData(
                new PoiModel
                {
                    Id = 1,
                    Ten = "Ốc Oanh",
                    MoTa_Vi = "Quán ốc nổi tiếng lâu năm trên phố ẩm thực Vĩnh Khánh.",
                    MoTa_En = "A long-standing famous seafood/snail restaurant on Vinh Khanh food street.",
                    MoTa_Zh = "位于永庆美食街的知名老牌海鲜店。",
                    Lat = 10.76147422883112,
                    Lng = 106.70258525764435,
                    BanKinh = 50,
                    UuTien = 1,
                    TrangThaiDuyet = "Approved",
                    TtsVoiceCode = "vi-VN"
                },
                new PoiModel
                {
                    Id = 2,
                    Ten = "Ốc Thảo",
                    MoTa_Vi = "Quán ốc đông khách trên đường Vĩnh Khánh.",
                    MoTa_En = "A popular seafood spot on Vinh Khanh street.",
                    MoTa_Zh = "永庆街上的人气海鲜店。",
                    Lat = 10.760980,
                    Lng = 106.703420,
                    BanKinh = 65,
                    UuTien = 2,
                    TrangThaiDuyet = "Approved",
                    TtsVoiceCode = "vi-VN"
                },
                new PoiModel
                {
                    Id = 3,
                    Ten = "Ốc Vũ",
                    MoTa_Vi = "Quán ốc mở khuya trong khu ẩm thực Vĩnh Khánh.",
                    MoTa_En = "A late-night seafood place in Vinh Khanh food area.",
                    MoTa_Zh = "永庆美食区内营业到深夜的海鲜店。",
                    Lat = 10.760760,
                    Lng = 106.703680,
                    BanKinh = 65,
                    UuTien = 3,
                    TrangThaiDuyet = "Approved",
                    TtsVoiceCode = "vi-VN"
                },
                new PoiModel
                {
                    Id = 4,
                    Ten = "Ốc Phát",
                    MoTa_Vi = "Quán ốc quen thuộc với thực khách địa phương.",
                    MoTa_En = "A familiar seafood eatery for locals.",
                    MoTa_Zh = "本地食客常去的海鲜店。",
                    Lat = 10.760420,
                    Lng = 106.704080,
                    BanKinh = 65,
                    UuTien = 4,
                    TrangThaiDuyet = "Approved",
                    TtsVoiceCode = "vi-VN"
                }
            );
        }
    }
}
