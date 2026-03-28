# App About — Thuyết minh tự động đa ngôn ngữ
## Phố ẩm thực Vĩnh Khánh

---

## 1. Giới thiệu

App About là ứng dụng mobile được xây dựng bằng .NET MAUI, cho phép tự động phát thuyết minh đa ngôn ngữ khi người dùng đi ngang qua các địa điểm ăn uống tại phố Vĩnh Khánh.

Cơ chế hoạt động:
- GPS tracking để lấy vị trí người dùng
- Geofence xác định khi người dùng gần POI
- Text-to-Speech phát thuyết minh tự động

---

## 2. Mục tiêu

- Trải nghiệm hands-free (không cần thao tác)
- Hoạt động offline-first
- Không phát trùng âm thanh (cooldown 5 phút)
- Kiến trúc rõ ràng, dễ bảo trì (MVVM + DI)

---

## 3. Công nghệ sử dụng

| Thành phần | Công nghệ |
|-----------|----------|
| Framework | .NET MAUI |
| Bản đồ | Mapsui (OpenStreetMap) |
| Database | SQLite |
| GPS | Geolocation API |
| Audio | Text-to-Speech |
| QR Code | ZXing.Net.MAUI |

---

## 4. Chức năng chính

### 4.1 Bản đồ và GPS
- Hiển thị vị trí người dùng
- Hiển thị danh sách POI trên bản đồ
- Cập nhật vị trí mỗi 3 giây

### 4.2 Geofence
- Tính khoảng cách bằng công thức Haversine
- Kích hoạt khi vào bán kính POI
- Cooldown 5 phút tránh lặp

### 4.3 Audio
- TTS đa ngôn ngữ (vi, en, zh)
- Hàng đợi audio (Queue)
- Không phát chồng âm thanh

### 4.4 QR Code
- Quét QR để phát thuyết minh ngay
- Bỏ qua Geofence và Cooldown

### 4.5 Offline và Sync
- Lưu dữ liệu bằng SQLite
- Đồng bộ từ API khi có mạng

---

## 5. Kiến trúc hệ thống

- MVVM (Model - View - ViewModel)
- Dependency Injection
- Layered Architecture

Luồng chính:
GPS → Geofence → Narration → SQLite

---

## 6. Data Model

### POI
- Id
- Ten
- MoTa_Vi / MoTa_En / MoTa_Zh
- Lat / Lng
- BanKinh
- UuTien

### LichSuPhat
- PoiId
- ThoiGianPhat
- NgonNgu
- NguonKichHoat

---

## 7. Sequence Diagram

User di chuyển → GPS cập nhật → Geofence kiểm tra → Audio phát
