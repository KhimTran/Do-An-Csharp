using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VinhKhanhApi.Data;

#nullable disable

namespace VinhKhanhApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260504120000_AddOwnerSubmissionStatus")]
    public partial class AddOwnerSubmissionStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrangThaiDeXuatOwner",
                table: "POIs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE POIs
SET TrangThaiDeXuatOwner =
    CASE
        WHEN TrangThaiDuyet = N'Pending' THEN N'Pending'
        WHEN TrangThaiDuyet = N'Rejected' THEN N'Rejected'
        ELSE TrangThaiDeXuatOwner
    END,
    TrangThaiDuyet = N'Approved'
WHERE TrangThaiDuyet IN (N'Pending', N'Rejected')
    AND (
        NgayDeXuat IS NOT NULL
        OR NoiDungDeXuat IS NOT NULL
        OR MoTaEnDeXuat IS NOT NULL
        OR MoTaZhDeXuat IS NOT NULL
        OR AudioFileViDeXuat IS NOT NULL
        OR AudioFileEnDeXuat IS NOT NULL
        OR AudioFileZhDeXuat IS NOT NULL
        OR ImagePathDeXuat IS NOT NULL
    )
    AND (
        LTRIM(RTRIM(ISNULL(MoTa_Vi, N''))) <> N''
        OR LTRIM(RTRIM(ISNULL(MoTa_En, N''))) <> N''
        OR LTRIM(RTRIM(ISNULL(MoTa_Zh, N''))) <> N''
        OR TenFileAudio_Vi IS NOT NULL
        OR TenFileAudio_En IS NOT NULL
        OR TenFileAudio_Zh IS NOT NULL
        OR TenFileAnhMinhHoa IS NOT NULL
    );

UPDATE POIs
SET TrangThaiDeXuatOwner = N'Pending'
WHERE TrangThaiDeXuatOwner IS NULL
    AND TrangThaiDuyet = N'Approved'
    AND NgayDeXuat IS NOT NULL
    AND NgayDuyet IS NULL
    AND (
        NoiDungDeXuat IS NOT NULL
        OR MoTaEnDeXuat IS NOT NULL
        OR MoTaZhDeXuat IS NOT NULL
        OR AudioFileViDeXuat IS NOT NULL
        OR AudioFileEnDeXuat IS NOT NULL
        OR AudioFileZhDeXuat IS NOT NULL
        OR ImagePathDeXuat IS NOT NULL
    );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrangThaiDeXuatOwner",
                table: "POIs");
        }
    }
}
