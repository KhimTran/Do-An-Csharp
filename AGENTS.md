# AGENTS.md — Hướng dẫn cho Codex agent

## Mục tiêu
Repo này là đồ án **.NET MAUI 10**. Khi sửa code, ưu tiên:
- Giữ project build đúng theo **.NET MAUI 10**
- Không tự ý downgrade SDK / workload
- Không đổi target framework nếu không được yêu cầu
- Sau khi sửa phải build kiểm tra lỗi compile
- Nếu sửa phần map, ưu tiên code chạy ổn định trên **điện thoại Android thật**

---

## Môi trường (Environment)

Môi trường agent có thể **không có sẵn dotnet** hoặc có dotnet nhưng **không đúng version**.

### Quy tắc bắt buộc
1. **Không mặc định cài .NET 9**
2. Repo này dùng **.NET MAUI 10**
3. Khi cần cài SDK, phải dùng **.NET SDK 10**
4. Khi build Android, phải dùng **`net10.0-android`**
5. Không tự ý đổi project xuống `net9.0-android`

---

## Quy trình bắt buộc trước khi sửa code

### Bước 1: Kiểm tra SDK hiện có
Chạy:

```bash
dotnet --info
dotnet --list-sdks

Nếu đã có SDK 10 phù hợp thì dùng luôn, không cài thêm SDK khác.

Bước 2: Nếu chưa có đúng SDK, cài .NET SDK 10
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir $HOME/.dotnet
Bước 3: Export PATH (phải chạy mỗi session nếu dùng SDK vừa cài)
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH
hash -r
Bước 4: Cài workload MAUI Android đúng với SDK hiện tại
$HOME/.dotnet/dotnet workload install maui-android --skip-sign-check

Nếu máy đã có dotnet đúng version và đã có workload tương thích thì không cần cài lại.

Build để kiểm tra lỗi C#
Restore packages
dotnet restore App/App/App.csproj
Build nhanh — chỉ kiểm tra lỗi compile, không tạo APK
dotnet build App/App/App.csproj \
  --framework net10.0-android \
  --configuration Debug \
  --verbosity minimal 2>&1 | tail -30
Build đầy đủ nếu cần log rõ hơn
dotnet build App/App/App.csproj \
  --framework net10.0-android \
  --configuration Debug \
  --verbosity minimal
Nguyên tắc build
Luôn build bằng net10.0-android
Không tự ý build bằng net9.0-android
Không đổi sang iOS / Windows nếu không cần
Sau khi sửa code, phải build lại
Nếu build fail, phải sửa tiếp cho tới khi:
compile pass, hoặc
nêu rõ blocker thật sự
Giới hạn của môi trường agent
Tác vụ	Trạng thái
Kiểm tra lỗi C# / XAML	✅ Dùng dotnet build
Chạy unit test	✅ Nếu có test project
Tạo APK	⚠️ Có thể cần Android SDK đầy đủ
Chạy app / chụp màn hình	❌ Không có emulator mặc định
Test GPS / camera / map trên điện thoại thật	❌ Agent không thay thế được thiết bị thật
Cấu trúc project
App/
├── App/
│   ├── App.csproj          ← file project chính
│   ├── MauiProgram.cs
│   ├── Models/
│   ├── ViewModels/
│   ├── Views/
│   └── Services/
└── App.slnx                ← solution file

Nếu cấu trúc repo thực tế khác, hãy ưu tiên cấu trúc thực tế trong repo.

Target framework hợp lệ

Repo này dùng .NET MAUI 10. Khi test trong agent, ưu tiên:

net10.0-android — dùng để test build trong agent
net10.0-ios — chỉ build được trên macOS
net10.0-windows10.0.19041.0 — chỉ build được trên Windows

Luôn dùng net10.0-android khi test trong agent.

Lưu ý NuGet packages

Project có thể dùng các package như:

CommunityToolkit.Maui
CommunityToolkit.Mvvm
thư viện map hiện tại hoặc mới
sqlite-net-pcl
SQLitePCLRaw.bundle_green
Quy tắc
Không tự ý nâng/downgrade package nếu không cần
Nếu thay thư viện map, phải xóa sạch code/tham chiếu cũ không còn dùng
Nếu restore lỗi network, nêu rõ lỗi; không được giả vờ đã build thành công
Quy định riêng cho phần Map

Nếu tác vụ liên quan đến bản đồ, phải tuân thủ:

Không quay lại Google Maps
Không giữ code chết của thư viện map cũ
Nếu chuyển sang Leaflet:
Bắt buộc dùng WebView hoặc HybridWebView
Phải đóng gói HTML/CSS/JS local trong app
Không dùng CDN
Không phụ thuộc localhost chỉ chạy trên emulator
Phải ưu tiên chạy ổn định trên điện thoại thật Android
Nếu API lỗi thì map vẫn phải hiện được dữ liệu local/offline nếu app có fallback
Quy tắc khi sửa code
Không thêm using dư thừa
Không xóa comment tiếng Việt có sẵn nếu chưa cần
Không đổi tên file/class bừa bãi làm vỡ binding / DI / XAML
Tôn trọng mô hình MVVM hiện có
Khi sửa XAML + code-behind, kiểm tra cả compile C# lẫn lỗi XAML
Nếu sửa asset local cho WebView/Leaflet, phải đảm bảo file được include đúng vào project
Sau khi sửa, PHẢI chạy dotnet build để xác nhận không có lỗi
Quy tắc xác nhận hoàn tất

Chỉ được coi là xong khi đã làm đủ:

Sửa code theo yêu cầu
Restore thành công hoặc nêu rõ blocker thật
Build lại project app bằng net10.0-android
Không còn lỗi compile do phần vừa sửa
Báo cáo rõ:
file nào đã sửa
file nào đã thêm/xóa
đã build bằng framework nào
kết quả build ra sao
Commit message

Dùng commit message ngắn gọn, rõ nghĩa, ưu tiên tiếng Việt.

Ví dụ:

feat: chuyển map sang leaflet local webview
fix: sửa lỗi build map page android
refactor: tách bridge csharp javascript cho leaflet