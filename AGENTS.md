🎯 Mục tiêu

Repo này là đồ án .NET MAUI 10 (C#).

Khi sửa code, agent phải:

Giữ project build được
Không phá kiến trúc hiện có
Ưu tiên chạy ổn định trên điện thoại Android thật
Code phải demo được, không chỉ “compile cho qua”
⚙️ Quy tắc nền tảng (BẮT BUỘC)
Không downgrade:
❌ net9.0
❌ SDK thấp hơn
Luôn dùng:
✅ net10.0-android
Không tự ý đổi:
Target framework
Kiến trúc project
Không xóa code khi chưa chắc chắn
Không thêm thư viện nếu không cần thiết
🏗️ Kiến trúc (Architecture Rules)

Project dùng MVVM

Layer	Vai trò
Models	Dữ liệu (POI, History...)
ViewModels	Logic
Views	UI
Services	GPS, DB, API, TTS
Bắt buộc:
Không nhét logic vào View (.xaml.cs)
Không gọi DB trực tiếp trong View
Logic phải nằm ở ViewModel hoặc Service
🔄 Quy tắc sửa code

Khi sửa bất kỳ feature nào:

1. Đọc trước khi sửa
Tìm hiểu flow hiện tại
Không sửa mù
2. Sửa tối thiểu
Chỉ sửa đúng phần cần
Không refactor lan rộng nếu không cần
3. Không phá code cũ
Nếu thay công nghệ → phải xóa sạch phần cũ
Không để “code chết”
📱 Quy tắc Android thật (CỰC QUAN TRỌNG)

Mọi feature phải ưu tiên:

✅ Chạy trên điện thoại thật
❌ Không chỉ chạy emulator
Cấm:
Hardcode localhost
Code chỉ chạy với 10.0.2.2
Bắt buộc:
API phải cấu hình được (IP LAN hoặc public)
Nếu API fail → phải có fallback local (SQLite / sample data)
🌐 Quy tắc làm việc với API
Không assume API luôn chạy
Phải handle:
mất mạng
timeout
lỗi JSON
Bắt buộc:
Có fallback:
SQLite
hoặc data mẫu
💾 Quy tắc dữ liệu (SQLite)
Không làm app phụ thuộc hoàn toàn server
POI phải load được offline
Khi có mạng → sync
Khi không có mạng → vẫn chạy
🗺️ Quy tắc Map (áp dụng nếu có)
Không dùng Google Maps
Nếu dùng Leaflet:
Bắt buộc WebView
HTML/CSS/JS local
Không dùng CDN
Nếu API lỗi → vẫn phải hiện map + POI local
📍 GPS / Sensor
Phải xin quyền đúng cách
Không crash nếu:
user từ chối quyền
GPS tắt
Phải handle null location
🔊 Audio / TTS
Không phát chồng âm
Có cơ chế stop / cancel
Không crash nếu:
không có voice phù hợp
📷 QR / Camera (nếu có)
Handle:
không có quyền camera
scan fail
Không crash khi không đọc được QR
⚡ Hiệu năng & ổn định
Không block UI thread
Luôn dùng async/await đúng cách
Không loop vô hạn
Không spam API/GPS
🧪 Quy tắc build & kiểm tra

Sau khi sửa:

BẮT BUỘC:
dotnet restore App/App/App.csproj
dotnet build App/App/App.csproj --framework net10.0-android --configuration Debug
Không được:
Báo “xong” khi chưa build
Fake build success
✅ Điều kiện hoàn thành

Chỉ được coi là xong khi:

Build PASS
Không lỗi compile
Feature chạy logic đúng
Không crash
📋 Báo cáo bắt buộc

Agent phải ghi rõ:

File đã sửa
File đã thêm
File đã xóa
Framework đã build
Kết quả build
🧠 Nguyên tắc quan trọng nhất

❗ Code phải chạy được trên điện thoại thật
❗ Không phụ thuộc môi trường dev
❗ Không làm kiểu “compile được nhưng không dùng được”

📝 Commit message

Ngắn gọn, rõ nghĩa:

feat: thêm qr scan
fix: sửa crash gps null
refactor: tách service map
