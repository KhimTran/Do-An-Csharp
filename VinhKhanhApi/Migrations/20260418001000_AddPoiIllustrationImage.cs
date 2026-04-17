using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhApi.Migrations
{
    public partial class AddPoiIllustrationImage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenFileAnhMinhHoa",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenFileAnhMinhHoa",
                table: "POIs");
        }
    }
}
