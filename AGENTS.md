# AGENTS.md — Hướng dẫn cho Codex agent

## Môi trường (Environment)

Môi trường agent KHÔNG có `dotnet` sẵn. Chạy lệnh sau TRƯỚC KHI làm bất cứ thứ gì:

```bash
# Bước 1: Cài .NET SDK 9
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0 --install-dir $HOME/.dotnet

# Bước 2: Export PATH (phải chạy mỗi session)
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

# Bước 3: Cài workload MAUI Android
$HOME/.dotnet/dotnet workload install maui-android --skip-sign-check
```

## Build để kiểm tra lỗi C#

```bash
# Build nhanh — chỉ kiểm tra lỗi compile, không tạo APK
dotnet build App/App/App.csproj \
  --framework net9.0-android \
  --configuration Debug \
  --verbosity minimal \
  --no-restore 2>&1 | tail -30
```

Nếu lần đầu (chưa restore packages):
```bash
dotnet restore App/App/App.csproj
dotnet build App/App/App.csproj -f net9.0-android -v minimal
```

## Giới hạn của môi trường agent

| Tác vụ | Được không? |
|---|---|
| Kiểm tra lỗi C# / XAML | ✅ Dùng `dotnet build` |
| Chạy unit test | ✅ Nếu có project test riêng |
| Tạo file APK | ⚠️ Cần Android SDK đầy đủ |
| Chạy app / chụp màn hình | ❌ Không có emulator |

## Cấu trúc project

```
App/
├── App/
│   ├── App.csproj          ← file project chính
│   ├── MauiProgram.cs
│   ├── Models/
│   ├── ViewModels/
│   ├── Views/
│   └── Services/
└── App.slnx                ← solution file
```

## Target framework hợp lệ

- `net9.0-android` — dùng để test build (không cần Mac)
- `net9.0-ios` — chỉ build được trên macOS
- `net9.0-windows10.0.19041.0` — chỉ build được trên Windows

**Luôn dùng `net9.0-android` khi test trong agent.**

## Lưu ý NuGet packages

Project dùng các package:
- `CommunityToolkit.Maui` 9.1.0
- `CommunityToolkit.Mvvm` 8.4.0
- `Mapsui.Maui` (bản đồ, không cần API key)
- `sqlite-net-pcl` + `SQLitePCLRaw.bundle_green`

Nếu restore bị lỗi network, bỏ qua và chỉ build những file đã thay đổi.

## Quy tắc khi sửa code

1. Không thêm `using` dư thừa
2. Không xóa comment tiếng Việt có sẵn
3. Sau khi sửa, PHẢI chạy `dotnet build` để xác nhận không có lỗi
4. Commit message dùng tiếng Việt, ví dụ: `feat: thêm geofence service`
