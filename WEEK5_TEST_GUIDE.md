# Hướng dẫn test để chốt **Tuần 5** (QR + CMS + Đồng bộ)

> Mục tiêu: sau khi chạy hết các bước dưới đây, bạn có thể tự tin nói với thầy là đã hoàn thành Tuần 5 ở mức MVP.

---

## 0) Điều kiện cần trước khi test

1. Máy dev đã cài:
   - .NET SDK (khuyến nghị đúng version theo project)
   - Visual Studio 2022/2026 + workload .NET MAUI (Android)
   - SQL Server LocalDB hoặc SQL Server Express
   - Android Emulator hoặc điện thoại Android thật
2. Repo đã pull mới nhất.

---

## 1) Test build cơ bản (bắt buộc)

Mở terminal tại root repo, chạy:

```bash
# Build backend API/CMS
 dotnet build VinhKhanhApi/VinhKhanhApi.slnx -v minimal

# Build app MAUI (Android)
 dotnet build App/App.slnx -v minimal
```

**Pass khi:** cả 2 lệnh `Build succeeded`.

---

## 2) Test CMS CRUD POI + upload audio

### 2.1 Chạy API/CMS

```bash
cd VinhKhanhApi
dotnet run
```

Mở trình duyệt vào URL in ra console (ví dụ `https://localhost:xxxx`), vào trang CMS mặc định `/Cms/Index`.

### 2.2 Test CRUD

1. **Create:** bấm “+ Thêm POI”, nhập:
   - Tên, mô tả VI/EN/ZH
   - Lat/Lng hợp lệ quanh Vĩnh Khánh
   - Bán kính (vd 50)
   - Ưu tiên
   - Upload 1 file audio (VI)
2. **Read:** sau lưu, POI mới xuất hiện trong danh sách.
3. **Update:** sửa tên hoặc bán kính, lưu lại.
4. **Delete:** xóa POI test.

**Pass khi:** tạo/sửa/xóa đều phản ánh đúng trên UI và DB.

---

## 3) Test API dữ liệu cho app sync

Giữ API đang chạy, test endpoint:

```bash
# lấy tất cả POI
curl http://localhost:<PORT>/api/pois

# lấy 1 POI
curl http://localhost:<PORT>/api/pois/1
```

**Pass khi:** trả JSON hợp lệ, có dữ liệu POI.

> Nếu test trên Android Emulator, nhớ đổi base URL app sang `http://10.0.2.2:<PORT>/api/pois`.

---

## 4) Test app MAUI: sync + ngôn ngữ + lịch sử

### 4.1 Chạy app Android

- Mở project MAUI trong Visual Studio.
- Chọn emulator hoặc máy thật.
- Run app.

### 4.2 Test đồng bộ

1. Tắt `offline mode` trong Settings.
2. Mở app lại (hoặc trigger luồng sync).
3. Vào danh sách/map kiểm tra POI từ server đã xuất hiện.

**Pass khi:** POI tạo từ CMS xuất hiện trong app.

### 4.3 Test cài đặt ngôn ngữ

1. Vào tab **Cài đặt**.
2. Đổi lần lượt `Tiếng Việt` → `English` → `中文`, bấm **Lưu**.
3. Đóng mở lại app.

**Pass khi:** ngôn ngữ đã chọn vẫn được giữ sau khi mở lại (đã lưu Preferences + SQLite).

### 4.4 Test lịch sử local

1. Kích hoạt phát thuyết minh (qua QR hoặc GPS).
2. Vào tab **Lịch sử**.

**Pass khi:** thấy bản ghi mới và top POI tăng số lượt nghe.

---

## 5) Test QR code end-to-end (quan trọng nhất tuần 5)

### 5.1 Chuẩn bị mã QR

- Tạo QR chỉ chứa số ID POI (ví dụ: `1`, `2`, `15`).
- Có thể dùng web tạo QR hoặc in ra giấy.

### 5.2 Quy trình test

1. Mở tab **QR** trên app.
2. Quét QR của POI tồn tại.
3. App phải:
   - nhận mã,
   - tìm đúng POI,
   - phát thuyết minh bằng ngôn ngữ đã chọn,
   - ghi lịch sử nguồn kích hoạt = `QR`.

### 5.3 Case âm cần test

- QR không phải số (`abc`) → báo “mã không hợp lệ”.
- QR là số nhưng POI không tồn tại (`9999`) → báo “không tìm thấy điểm”.

**Pass khi:** cả case dương + case âm đều xử lý đúng như trên.

---

## 6) Test trang thống kê CMS

Sau khi có dữ liệu nghe (log), mở `/Cms/Stats`.

**Pass khi:**
- Tổng lượt nghe > 0,
- Thời lượng trung bình hiển thị,
- Bảng top POI có dữ liệu.

---

## 7) Bộ minh chứng nên nộp cho thầy (rất nên làm)

1. **Video 2–4 phút** gồm:
   - Tạo POI trên CMS,
   - App sync thấy POI,
   - Quét QR phát thuyết minh,
   - Mở lịch sử app,
   - Mở stats CMS.
2. **Ảnh chụp màn hình** từng bước chính.
3. **Checklist có tick ✅** cho từng mục Tuần 5.

---

## 8) Checklist chốt “Hoàn thành Tuần 5”

- [ ] Build được API + App.
- [ ] CMS CRUD POI chạy ổn.
- [ ] Upload audio hoạt động.
- [ ] API `/api/pois` trả dữ liệu đúng.
- [ ] App sync POI từ server về local.
- [ ] Chọn ngôn ngữ vi/en/zh và lưu lại sau khi mở lại app.
- [ ] Quét QR trigger thuyết minh ngay.
- [ ] Lịch sử local ghi nhận đúng POI/thời gian/nguồn.
- [ ] Trang thống kê CMS hiện số liệu đúng.

Nếu tick đủ 9 mục trên => có thể tự tin kết luận **đã hoàn thành Tuần 5 (MVP)**.
