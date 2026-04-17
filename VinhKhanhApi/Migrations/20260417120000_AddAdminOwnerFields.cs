using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhApi.Migrations
{
    public partial class AddAdminOwnerFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "GalleryJson", table: "POIs", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "GioDongCua", table: "POIs", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "GioMoCua", table: "POIs", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "LyDoTuChoi", table: "POIs", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "MonDacTrung", table: "POIs", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "NgayDeXuat", table: "POIs", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "NgayDuyet", table: "POIs", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "NguoiCapNhat", table: "POIs", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "NoiDungDeXuat", table: "POIs", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "QrCodeNoiDung", table: "POIs", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SoDienThoai", table: "POIs", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "TrangThaiDuyet", table: "POIs", type: "nvarchar(max)", nullable: false, defaultValue: "Approved");
            migrationBuilder.AddColumn<string>(name: "TtsVoiceCode", table: "POIs", type: "nvarchar(max)", nullable: true);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "TrangThaiDuyet", "TtsVoiceCode" },
                values: new object[] { "Approved", "vi-VN" });

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "TrangThaiDuyet", "TtsVoiceCode" },
                values: new object[] { "Approved", "vi-VN" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GalleryJson", table: "POIs");
            migrationBuilder.DropColumn(name: "GioDongCua", table: "POIs");
            migrationBuilder.DropColumn(name: "GioMoCua", table: "POIs");
            migrationBuilder.DropColumn(name: "LyDoTuChoi", table: "POIs");
            migrationBuilder.DropColumn(name: "MonDacTrung", table: "POIs");
            migrationBuilder.DropColumn(name: "NgayDeXuat", table: "POIs");
            migrationBuilder.DropColumn(name: "NgayDuyet", table: "POIs");
            migrationBuilder.DropColumn(name: "NguoiCapNhat", table: "POIs");
            migrationBuilder.DropColumn(name: "NoiDungDeXuat", table: "POIs");
            migrationBuilder.DropColumn(name: "QrCodeNoiDung", table: "POIs");
            migrationBuilder.DropColumn(name: "SoDienThoai", table: "POIs");
            migrationBuilder.DropColumn(name: "TrangThaiDuyet", table: "POIs");
            migrationBuilder.DropColumn(name: "TtsVoiceCode", table: "POIs");
        }
    }
}
