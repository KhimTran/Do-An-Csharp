# DoAnCsharp

Do an gom hai project:

- `App/App/App.csproj`: ung dung .NET MAUI 10, uu tien Android that.
- `VinhKhanhApi/VinhKhanhApi.csproj`: ASP.NET Core 10 API cho du lieu POI/QR.

## Yeu cau moi truong

- .NET SDK 10.x.
- MAUI workload de build `net10.0-android`.
- Android SDK/JDK da cau hinh neu build hoac deploy app Android.
- SQL Server LocalDB hoac SQL Server tuong thich neu chay API voi connection string mac dinh.

## Build app MAUI Android

Chay tu thu muc goc repo:

```powershell
dotnet restore App/App/App.csproj
dotnet build App/App/App.csproj --framework net10.0-android --configuration Debug
```

Khong doi target framework ve `net9.0`. App Android phai giu `net10.0-android`.

## Build API

```powershell
dotnet restore VinhKhanhApi/VinhKhanhApi.csproj
dotnet build VinhKhanhApi/VinhKhanhApi.csproj --configuration Debug
```

## Chay API de dien thoai that goi duoc

API co profile HTTP lang nghe tren port `5099`.

```powershell
dotnet run --project VinhKhanhApi/VinhKhanhApi.csproj --launch-profile http
```

Tren dien thoai Android that, khong dung `localhost`. Dung IP LAN cua may dang chay API, vi du:

```text
http://192.168.1.20:5099
```

Neu dung QR cau hinh server, QR nen tro den host goc, `/api`, hoac `/api/pois`.

## Quy tac offline

App khong duoc phu thuoc hoan toan vao API:

- Co mang: sync POI tu API ve SQLite local.
- Mat mang/API loi: doc POI tu SQLite hoac sample data.
- Timeout, loi JSON, HTTP error phai duoc handle va khong lam app crash.

## Map

Map dung Leaflet trong WebView:

- Asset local nam o `App/App/Resources/Raw/map`.
- Khong dung Google Maps.
- Khong dung CDN.
- Neu API loi, map van phai hien voi POI local.

## Cau truc nhanh

```text
App/App/Models              Model du lieu app
App/App/ViewModels          Logic MVVM
App/App/Views               XAML UI
App/App/Services            GPS, DB, API sync, TTS, QR, helpers
App/App/Resources/Raw/map   Leaflet local assets
VinhKhanhApi/Controllers    API endpoints
VinhKhanhApi/Data           DbContext va cau hinh DB
VinhKhanhApi/Migrations     EF Core migrations
VinhKhanhApi/Services       Logic backend
```

## CI

- `.github/workflows/vinhkhanhapi-ci.yml`: restore/build API.
- `.github/workflows/maui-android-ci.yml`: restore/build app MAUI Android.

## Luu y cho agent

Doc `AGENTS.md` truoc khi sua code. Sau khi sua app/API, phai chay dung lenh build theo pham vi da sua va bao cao ket qua that.

