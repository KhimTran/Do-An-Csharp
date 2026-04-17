using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VinhKhanhApi.Data;
using VinhKhanhApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    });

builder.Services.AddAuthorization();
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
    await DamBaoBangTaiKhoanAsync(db);
}

app.UseCors("AllowAll");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

static async Task DamBaoCotPoiMoiAsync(AppDbContext db)
{
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

static async Task DamBaoBangTaiKhoanAsync(AppDbContext db)
{
    var createTableSql = @"
IF OBJECT_ID('UserAccounts', 'U') IS NULL
BEGIN
    CREATE TABLE UserAccounts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(200) NOT NULL,
        Role NVARCHAR(20) NOT NULL,
        PoiId INT NULL,
        IsActive BIT NOT NULL DEFAULT(1),
        CreatedAt DATETIME2 NOT NULL DEFAULT(GETUTCDATE())
    );
END";

    await db.Database.ExecuteSqlRawAsync(createTableSql);

    var adminHash = PasswordHasher.Hash("admin123");
    var ownerHash = PasswordHasher.Hash("owner123");

    await db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (SELECT 1 FROM UserAccounts WHERE Username = 'admin')
BEGIN
    INSERT INTO UserAccounts (Username, PasswordHash, Role, PoiId, IsActive)
    VALUES ('admin', {adminHash}, 'Admin', NULL, 1);
END");

    await db.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (SELECT 1 FROM UserAccounts WHERE Username = 'owner1')
BEGIN
    INSERT INTO UserAccounts (Username, PasswordHash, Role, PoiId, IsActive)
    VALUES ('owner1', {ownerHash}, 'Owner', 1, 1);
END");
}
