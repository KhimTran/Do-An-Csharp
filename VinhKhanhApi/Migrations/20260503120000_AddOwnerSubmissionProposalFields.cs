using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VinhKhanhApi.Data;

#nullable disable

namespace VinhKhanhApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260503120000_AddOwnerSubmissionProposalFields")]
    public partial class AddOwnerSubmissionProposalFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioFileEnDeXuat",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFileViDeXuat",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFileZhDeXuat",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePathDeXuat",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoTaEnDeXuat",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoTaZhDeXuat",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioFileEnDeXuat",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioFileViDeXuat",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioFileZhDeXuat",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "ImagePathDeXuat",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "MoTaEnDeXuat",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "MoTaZhDeXuat",
                table: "POIs");
        }
    }
}
