using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VinhKhanhApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "POIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa_Vi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa_Zh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lat = table.Column<double>(type: "float", nullable: false),
                    Lng = table.Column<double>(type: "float", nullable: false),
                    BanKinh = table.Column<double>(type: "float", nullable: false),
                    UuTien = table.Column<int>(type: "int", nullable: false),
                    TenFileAudio_Vi = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POIs", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "POIs",
                columns: new[] { "Id", "BanKinh", "Lat", "Lng", "MoTa_En", "MoTa_Vi", "MoTa_Zh", "Ten", "TenFileAudio_Vi", "UuTien" },
                values: new object[,]
                {
                    { 1, 50.0, 10.76147422883112, 106.70258525764435, "A long-standing famous seafood/snail restaurant on Vinh Khanh food street.", "Quán ốc nổi tiếng lâu năm trên phố ẩm thực Vĩnh Khánh.", "位于永庆美食街的知名老牌海鲜店。", "Ốc Oanh", null, 1 },
                    { 2, 65.0, 10.76098, 106.70342, "A popular seafood spot on Vinh Khanh street.", "Quán ốc đông khách trên đường Vĩnh Khánh.", "永庆街上的人气海鲜店。", "Ốc Thảo", null, 2 },
                    { 3, 65.0, 10.76076, 106.70368, "A late-night seafood place in Vinh Khanh food area.", "Quán ốc mở khuya trong khu ẩm thực Vĩnh Khánh.", "永庆美食区内营业到深夜的海鲜店。", "Ốc Vũ", null, 3 },
                    { 4, 65.0, 10.76042, 106.70408, "A familiar seafood eatery for locals.", "Quán ốc quen thuộc với thực khách địa phương.", "本地食客常去的海鲜店。", "Ốc Phát", null, 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "POIs");
        }
    }
}
