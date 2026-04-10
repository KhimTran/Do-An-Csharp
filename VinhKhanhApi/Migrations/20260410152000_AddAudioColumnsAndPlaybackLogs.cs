using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhApi.Migrations
{
    public partial class AddAudioColumnsAndPlaybackLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenFileAudio_En",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenFileAudio_Zh",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlaybackLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoiId = table.Column<int>(type: "int", nullable: false),
                    PoiTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nguon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThoiLuongGiay = table.Column<int>(type: "int", nullable: false),
                    ThoiGianNghe = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybackLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaybackLogs");

            migrationBuilder.DropColumn(
                name: "TenFileAudio_En",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "TenFileAudio_Zh",
                table: "POIs");
        }
    }
}
