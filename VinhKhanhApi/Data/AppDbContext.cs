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
        public DbSet<RoutePingModel> RoutePings { get; set; }
        public DbSet<UserAccountModel> UserAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PoiModel>().HasData(
                new PoiModel
                {
                    Id = 1,
                    Ten = "Quán Bún Bò Huế Vĩnh Khánh",
                    MoTa_Vi = "Quán bún bò nổi tiếng với hơn 30 năm lịch sử.",
                    MoTa_En = "Famous bun bo Hue restaurant with 30-year history.",
                    MoTa_Zh = "著名的顺化牛肉米线餐厅，拥有30年历史。",
                    Lat = 10.7565,
                    Lng = 106.6896,
                    BanKinh = 50,
                    UuTien = 1,
                    TrangThaiDuyet = "Approved",
                    TtsVoiceCode = "vi-VN"
                },
                new PoiModel
                {
                    Id = 2,
                    Ten = "Chợ Xóm Chiếu",
                    MoTa_Vi = "Khu chợ truyền thống lâu đời của quận 4.",
                    MoTa_En = "Traditional market of District 4.",
                    MoTa_Zh = "第四郡传统市场。",
                    Lat = 10.7580,
                    Lng = 106.6910,
                    BanKinh = 80,
                    UuTien = 2,
                    TrangThaiDuyet = "Approved",
                    TtsVoiceCode = "vi-VN"
                }
            );
        }
    }
}
