# Cách test **tính năng Tuần 5 của Vinh** (Web CMS)

> Phạm vi Vinh phụ trách tuần 5: **CMS Razor + thống kê + kết nối SQL Server + kiểm tra đồng bộ app ↔ server**.

## 1) Chuẩn bị môi trường

- SQL Server LocalDB/Express chạy được.
- Mở `VinhKhanhApi/appsettings.json`, kiểm tra `DefaultConnection` đúng máy bạn.
- Có sẵn 1 trình duyệt (Edge/Chrome) + Postman hoặc `curl`.

## 2) Chạy CMS/API

```bash
cd VinhKhanhApi
dotnet run
```

- Mở URL app (ví dụ `https://localhost:xxxx`).
- Vào `https://localhost:xxxx/Cms/Index`.

**Pass:** trang CMS load thành công, không lỗi 500.

## 3) Test CRUD POI (phần quan trọng nhất của Vinh)

### 3.1 Create
1. Bấm **+ Thêm POI**.
2. Nhập dữ liệu mẫu:
   - Tên: `POI Test Vinh`
   - Mô tả VI/EN/ZH đầy đủ
   - Lat/Lng hợp lệ
   - Bán kính: `50`
   - Ưu tiên: `1`
3. Upload audio VI (mp3/wav bất kỳ).
4. Bấm lưu.

**Pass:**
- Quay về danh sách thấy POI mới.
- Cột Audio VI có tên file.

### 3.2 Update
1. Bấm **Sửa** POI vừa tạo.
2. Đổi tên thành `POI Test Vinh Updated`.
3. Lưu lại.

**Pass:** danh sách hiển thị tên mới.

### 3.3 Delete
1. Bấm **Xóa** POI test.
2. Xác nhận mất khỏi danh sách.

**Pass:** POI không còn trong bảng.

## 4) Test API backend (để chứng minh đồng bộ app↔server)

```bash
# tất cả POI
curl https://localhost:<PORT>/api/pois -k

# theo id
curl https://localhost:<PORT>/api/pois/1 -k
```

**Pass:** trả JSON hợp lệ, dữ liệu đúng thứ tự ưu tiên.

## 5) Test trang thống kê của Vinh

- Mở `https://localhost:<PORT>/Cms/Stats`.

**Pass khi có dữ liệu log:**
- Hiện **Tổng lượt nghe**.
- Hiện **Thời lượng trung bình**.
- Có bảng **Top POI**.

> Nếu chưa có log, tạo log bằng API:

```bash
curl -X POST https://localhost:<PORT>/api/analytics/logs -H "Content-Type: application/json" -d "{\"poiId\":1,\"poiTen\":\"POI Test Vinh\",\"thoiLuongGiay\":42}" -k
```

Sau đó reload `/Cms/Stats`.

## 6) Test kết nối app ↔ server (phần phối hợp với Khiêm)

1. Giữ API của Vinh đang chạy.
2. Bảo Khiêm mở app MAUI, tắt Offline mode.
3. Trigger sync trên app.
4. Kiểm tra POI vừa tạo ở CMS có xuất hiện trong app.

**Pass:** CMS tạo/sửa POI thì app sync và thấy dữ liệu tương ứng.

## 7) Minh chứng Vinh nên nộp

- Ảnh 1: CMS danh sách POI (sau Create/Update).
- Ảnh 2: Form Edit có upload audio.
- Ảnh 3: Trang Stats có số liệu.
- Ảnh 4: API `/api/pois` trả JSON.
- Video ngắn 1-2 phút: Create POI → app sync thấy POI.

## 8) Checklist chấm riêng phần Vinh

- [ ] CMS mở được và không lỗi.
- [ ] CRUD POI chạy đủ Create/Update/Delete.
- [ ] Upload audio lưu được tên file.
- [ ] `/api/pois` trả JSON đúng.
- [ ] `/Cms/Stats` hiển thị số liệu.
- [ ] Có minh chứng app sync đọc được POI từ CMS.

Tick đủ 6 mục trên = phần Tuần 5 của Vinh đạt yêu cầu MVP.
