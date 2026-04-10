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


// Quick schema guard: auto-add missing columns/tables on startup for local dev/demo.
using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    try
    {
        db.Database.ExecuteSqlRaw(@"IF COL_LENGTH('POIs', 'TenFileAudio_En') IS NULL ALTER TABLE POIs ADD TenFileAudio_En nvarchar(max) NULL;");
        db.Database.ExecuteSqlRaw(@"IF COL_LENGTH('POIs', 'TenFileAudio_Zh') IS NULL ALTER TABLE POIs ADD TenFileAudio_Zh nvarchar(max) NULL;");

        db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('PlaybackLogs', 'U') IS NULL
BEGIN
    CREATE TABLE [PlaybackLogs] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PoiId] int NOT NULL,
        [PoiTen] nvarchar(max) NOT NULL,
        [NguonKichHoat] nvarchar(max) NOT NULL,
        [NgonNgu] nvarchar(max) NOT NULL,
        [ThoiLuongGiay] int NOT NULL,
        [ThoiGianNghe] datetime2 NOT NULL
    );
END");
    }
    catch
    {
        // Do not block app startup in local environment if patch scripts fail.
    }
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cms}/{action=Index}/{id?}");

app.Run();
