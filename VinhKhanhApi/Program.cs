using Microsoft.EntityFrameworkCore;
using VinhKhanhApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DamBaoCotPoiMoiAsync(db);
}

app.UseCors("AllowAll");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cms}/{action=Index}/{id?}");

app.Run();

static async Task DamBaoCotPoiMoiAsync(AppDbContext db)
{
    // Fallback an toàn cho DB cũ: nếu cột chưa có thì thêm bằng SQL.
    var scripts = new[]
    {
        "IF COL_LENGTH('POIs','SoDienThoai') IS NULL ALTER TABLE POIs ADD SoDienThoai NVARCHAR(MAX) NULL;",
        "IF COL_LENGTH('POIs','GioMoCua') IS NULL ALTER TABLE POIs ADD GioMoCua NVARCHAR(MAX) NULL;",
        "IF COL_LENGTH('POIs','GioDongCua') IS NULL ALTER TABLE POIs ADD GioDongCua NVARCHAR(MAX) NULL;",
        "IF COL_LENGTH('POIs','MonDacTrung') IS NULL ALTER TABLE POIs ADD MonDacTrung NVARCHAR(MAX) NULL;",
        "IF COL_LENGTH('POIs','GalleryJson') IS NULL ALTER TABLE POIs ADD GalleryJson NVARCHAR(MAX) NULL;",
        "IF COL_LENGTH('POIs','QrCodeNoiDung') IS NULL ALTER TABLE POIs ADD QrCodeNoiDung NVARCHAR(MAX) NULL;",
        "IF COL_LENGTH('POIs','TtsVoiceCode') IS NULL ALTER TABLE POIs ADD TtsVoiceCode NVARCHAR(MAX) NULL;",
        "IF COL_LENGTH('POIs','NguoiCapNhat') IS NULL ALTER TABLE POIs ADD NguoiCapNhat NVARCHAR(MAX) NULL;",
        "IF COL_LENGTH('POIs','NoiDungDeXuat') IS NULL ALTER TABLE POIs ADD NoiDungDeXuat NVARCHAR(MAX) NULL;",
        "IF COL_LENGTH('POIs','TrangThaiDuyet') IS NULL ALTER TABLE POIs ADD TrangThaiDuyet NVARCHAR(MAX) NOT NULL CONSTRAINT DF_POIs_TrangThaiDuyet DEFAULT 'Approved';",
        "IF COL_LENGTH('POIs','NgayDeXuat') IS NULL ALTER TABLE POIs ADD NgayDeXuat DATETIME2 NULL;",
        "IF COL_LENGTH('POIs','NgayDuyet') IS NULL ALTER TABLE POIs ADD NgayDuyet DATETIME2 NULL;",
        "IF COL_LENGTH('POIs','LyDoTuChoi') IS NULL ALTER TABLE POIs ADD LyDoTuChoi NVARCHAR(MAX) NULL;"
    };

    foreach (var sql in scripts)
        await db.Database.ExecuteSqlRawAsync(sql);
}
