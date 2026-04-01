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
                    { 1, 50.0, 10.756500000000001, 106.6896, "Famous bun bo Hue restaurant with 30-year history.", "Quán bún bò nổi tiếng với hơn 30 năm lịch sử.", "著名的顺化牛肉米线餐厅，拥有30年历史。", "Quán Bún Bò Huế Vĩnh Khánh", null, 1 },
                    { 2, 80.0, 10.757999999999999, 106.691, "Traditional market of District 4.", "Khu chợ truyền thống lâu đời của quận 4.", "第四郡传统市场。", "Chợ Xóm Chiếu", null, 2 }
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
