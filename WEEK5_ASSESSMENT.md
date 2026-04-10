# Đánh giá codebase theo yêu cầu Tuần 5 (MVP)

Ngày đánh giá: 2026-04-10 (UTC)

## Kết luận nhanh
- **Đạt phần lớn yêu cầu Tuần 5 về mặt mã nguồn**: có QR scan + trigger thuyết minh, có chọn ngôn ngữ và lưu SQLite/Preferences, có lịch sử nghe local, có CMS Razor CRUD POI + upload audio + trang thống kê cơ bản.
- **Chưa thể xác nhận 100% end-to-end trong môi trường hiện tại** vì không có `dotnet` SDK để build/chạy, và không thể test QR/GPS/CMS thực tế trên thiết bị.

## Đối chiếu từng mục tuần 5

### 1) QR Code quét và trigger thuyết minh ngay
**Trạng thái: Có trong code (cần test thiết bị để chốt).**

- App đã tích hợp ZXing camera scanner (`CameraBarcodeReaderView`) với sự kiện `BarcodesDetected`.
- ViewModel parse POI ID từ QR, truy vấn POI trong SQLite, chọn ngôn ngữ và gọi TTS phát ngay.
- Có debounce đơn giản chống quét lặp trong 3 giây.

### 2) Giao diện chọn ngôn ngữ (vi/en/zh) và lưu preference vào SQLite
**Trạng thái: Đạt trong code.**

- Có `SettingsPage` với picker 3 ngôn ngữ, slider bán kính geofence, switch offline.
- Khi lưu: ghi vào `Preferences` và đồng thời ghi vào bảng settings SQLite (`AppSettingModel`).

### 3) Lưu lịch sử nghe (POI, thời gian, số lần)
**Trạng thái: Đạt trong code.**

- Khi trigger từ QR, app ghi lịch sử vào `LichSuPhatModel`.
- Có màn hình lịch sử + top POI, dữ liệu được group/count từ SQLite.

### 4) Web CMS (ASP.NET Razor) CRUD POI, upload audio, quản lý bản dịch
**Trạng thái: Đạt mức CRUD cơ bản trong code.**

- Có `CmsController` + `Index/Edit/Delete`, form có các trường mô tả đa ngôn ngữ.
- Có upload file audio vi/en/zh vào `wwwroot/audio`.

### 5) Trang thống kê
**Trạng thái: Đạt mức cơ bản trong code.**

- Có action `Stats` và view hiển thị tổng lượt nghe + thời lượng trung bình + top POI theo số lượt.

### 6) Kết nối CMS với SQL Server và luồng đồng bộ app ↔ server
**Trạng thái: Có cấu hình/logic, cần chạy thực tế để chốt.**

- API backend dùng EF Core SQL Server (`UseSqlServer`).
- App có `SyncService` gọi `GET /api/pois` và upsert vào SQLite local.
- Cần test runtime để xác nhận cổng/API/DB thực tế vận hành ổn định.

## Các mục chưa thể xác nhận trong môi trường hiện tại
1. Build/run App MAUI và API backend thành công trên máy thật.
2. Quét QR bằng camera thật, phát TTS đúng ngôn ngữ trên thiết bị Android.
3. Flow đồng bộ app ↔ API (mạng thật, DB SQL Server thật).
4. KPI “test QR thực tế” và “deploy Azure/Render”.

## Khuyến nghị chốt Tuần 5
1. Chạy build trên máy dev có .NET SDK (`dotnet build` cho MAUI và API).
2. Test checklist end-to-end:
   - Tạo POI trên CMS → app sync về SQLite.
   - In QR ID POI → quét trên app → phát thuyết minh đúng ngôn ngữ.
   - Mở trang thống kê CMS kiểm tra log tăng.
3. Nếu cần nộp gấp: ưu tiên chứng minh 3 luồng core ở trên bằng video ngắn.
