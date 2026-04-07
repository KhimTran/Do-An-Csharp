# Hướng dẫn đồ án .NET MAUI (Mapsui, không cần API Key)

> Đồ án: **Thuyết minh tự động đa ngôn ngữ — Phố ẩm thực Vĩnh Khánh**

Tài liệu này gom lại kế hoạch 6 tuần, phân công vai trò, checklist và phần kiến thức cốt lõi để 2 thành viên có thể học/chạy đúng tiến độ.

## 1) Mục tiêu tổng quát

- Xây ứng dụng MAUI có khả năng:
  - Theo dõi GPS liên tục.
  - Geofence quanh POI bằng công thức Haversine.
  - Tự phát thuyết minh đa ngôn ngữ (vi/en/zh).
  - Chạy offline-first với SQLite.
  - Hiển thị bản đồ bằng **Mapsui/OpenStreetMap** (miễn phí, không cần Google API key).

## 2) Nguyên tắc làm nhóm

- Mỗi tối **thứ 6** demo chéo: Khiêm giải thích code của Vinh và ngược lại.
- **Tuần 4 hoán đổi**: mỗi người refactor/giải thích phần của người còn lại.
- Dùng Git nghiêm túc:
  - Commit message rõ ràng.
  - Review PR trước khi merge vào `main`.
- Cuối tuần ghi lại 1 trang: “tuần này làm được gì, học được gì”.

## 3) Kế hoạch 6 tuần

### Tuần 1 — Nền tảng (MVVM + SQLite)

**Khiêm (Lead Dev)**
- Tạo project MAUI, cấu hình DI trong `MauiProgram.cs`.
- Tạo cấu trúc `Models / ViewModels / Views / Services`.
- Tạo `PoiModel`, `LocalDatabase` và CRUD SQLite cơ bản.

**Vinh (Dev UI)**
- Thiết kế `AppShell` + điều hướng Tab.
- Dựng UI danh sách POI (`CollectionView`) + binding VM.
- Khai báo quyền GPS Android/iOS.

**Mục tiêu tuần**
- App chạy được và đọc POI mẫu từ SQLite.

### Tuần 2 — GPS Tracking + Mapsui

**Khiêm (GPS Logic)**
- `Geolocation.GetLocationAsync` + tracking mỗi ~3 giây.
- Foreground Service Android (`LocationForegroundService`).
- Notification channel + start/stop service.

**Vinh (Mapsui UI)**
- Cài `Mapsui.Maui`.
- Tích hợp `MapControl` trong `MapPage`.
- Hiển thị OpenStreetMap tile + POI point features.
- Vẽ vòng geofence và xử lý tap POI.

**Mục tiêu tuần**
- Hiển thị bản đồ OSM + vị trí + POI đúng vị trí thực tế.

### Tuần 3 — Geofence + TTS tự động (cốt lõi)

**Khiêm (Geofence)**
- Công thức Haversine (đơn vị mét).
- Debounce/Cooldown chống phát lặp (HashSet + thời gian chờ).
- Kết nối luồng: GPS → Geofence → Trigger.

**Vinh (Audio/TTS)**
- `TextToSpeech.SpeakAsync` + locale vi/en/zh.
- Queue âm thanh chống chồng tiếng.
- (Tùy chọn) phát file mp3 bằng CommunityToolkit Audio.

**Mục tiêu tuần**
- Đi vào vùng POI thì tự phát thuyết minh chính xác.

### Tuần 4 — Đồng bộ + Offline (tuần hoán đổi)

**Khiêm (Backend/Sync)**
- API ASP.NET Core `/api/pois` trả JSON.
- `SyncService`: check mạng, tải JSON, upsert SQLite.
- Quản lý tải/lưu file audio offline.

**Vinh (Hoán đổi/Fix)**
- Đọc/refactor code GPS/Geofence của Khiêm.
- Tối ưu pin (giảm poll khi đứng yên).
- Làm màn hình Settings (ngôn ngữ, bán kính, offline mode).

### Tuần 5 — QR + CMS Web

**Khiêm (App)**
- Quét QR bằng `ZXing.Net.MAUI` để trigger POI ngay.
- Màn chọn ngôn ngữ, lưu preference.
- Lưu lịch sử nghe local (analytics cơ bản).

**Vinh (CMS)**
- Razor web CRUD POI + upload audio + bản dịch.
- Trang thống kê top POI, lịch sử dùng.
- Kết nối SQL Server và kiểm tra đồng bộ app/server.

### Tuần 6 — Hoàn thiện + Báo cáo

**Khiêm**
- Bugfix edge cases (mất GPS, mất mạng, từ chối quyền).
- Viết báo cáo kiến trúc và module.
- Chuẩn bị slide + demo flow.

**Vinh**
- Polish UI/UX (loading/empty/error states).
- Viết tài liệu sử dụng + bổ sung comment code.
- Test Android/iOS và quay video demo.

---

## 4) Gói NuGet đề xuất

- `CommunityToolkit.Maui`
- `CommunityToolkit.Mvvm`
- `sqlite-net-pcl`
- `SQLitePCLRaw.bundle_green`
- `Mapsui.Maui`
- (Tuần sau) `ZXing.Net.MAUI`

## 5) Kiến thức nền cần nắm

- Kiểu dữ liệu cơ bản: `string`, `int`, `double`, `bool`, `var`.
- `async/await`: không block UI khi đợi GPS/API/DB.
- MVVM:
  - **Model**: dữ liệu POI.
  - **View**: XAML UI.
  - **ViewModel**: logic kết nối View + Services.
- Dependency Injection:
  - `AddSingleton`: service dùng lâu dài (DB, GPS...).
  - `AddTransient`: tạo mới khi mở page/VM.

## 6) Câu hỏi bảo vệ thường gặp (trả lời ngắn)

- **Vì sao Mapsui thay Google Maps?**
  - Miễn phí, không cần API key/billing, đủ dùng cho đồ án.
- **Haversine là gì?**
  - Công thức tính khoảng cách theo bề mặt cong Trái Đất từ lat/lng.
- **Vì sao cần Foreground Service trên Android?**
  - Tránh OS kill tiến trình nền khi tắt màn hình.
- **HashSet trong geofence để làm gì?**
  - Đánh dấu POI đã phát để chống phát lặp khi đứng yên.
- **SQLite dùng làm gì?**
  - Lưu dữ liệu POI + lịch sử phát để app chạy offline.

## 7) Checklist theo tuần

### Tuần 1
- [ ] App build/chạy Android/iOS.
- [ ] DB tự tạo lần đầu.
- [ ] Hiển thị ≥ 2 POI mẫu.
- [ ] Nút “Tải lại” hoạt động.
- [ ] Cả 2 giải thích được MVVM + DI.

### Tuần 2
- [ ] Mapsui hiển thị OSM không cần API key.
- [ ] GPS lấy vị trí thực trên điện thoại.
- [ ] POI pin đúng tọa độ.
- [ ] Foreground service chạy khi tắt màn hình.

### Tuần 3
- [ ] Haversine test đúng với 2 điểm biết trước.
- [ ] Vào vùng POI thì tự phát thuyết minh.
- [ ] TTS phát được tiếng Việt + tiếng Anh.
- [ ] Cơ chế chống phát lặp hoạt động.

### Tuần 4
- [ ] Hoán đổi code review hoàn tất.
- [ ] API trả JSON POI.
- [ ] Đồng bộ app ↔ backend hoạt động.
- [ ] Settings (ngôn ngữ, bán kính) hoạt động.

### Tuần 5–6
- [ ] Quét QR trigger đúng POI.
- [ ] CMS CRUD cơ bản chạy được.
- [ ] Báo cáo + slide + video demo hoàn chỉnh.
- [ ] Cả 2 luyện bảo vệ ít nhất 3 lần.

## 8) Gợi ý thứ tự triển khai nhanh

1. Chốt `PoiModel`, `LocalDatabase`, seed dữ liệu mẫu.
2. Làm `PoiListPage` để kiểm tra dữ liệu đã đi xuyên suốt.
3. Tích hợp GPS + Mapsui.
4. Đưa geofence + TTS vào Map flow.
5. Mới thêm Sync, QR, CMS.

> Mẹo: luôn ưu tiên demo chạy được end-to-end trước, rồi mới tối ưu giao diện/performance.
