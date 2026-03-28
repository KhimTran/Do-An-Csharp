📄 README.md — Bản nâng cấp (xịn)
# App About — Multi-language Audio Guide
## Vĩnh Khánh Food Street

---

## 1. Overview

App About là ứng dụng mobile được phát triển bằng .NET MAUI, cung cấp trải nghiệm hướng dẫn viên ảo bằng cách tự động phát thuyết minh đa ngôn ngữ dựa trên vị trí GPS của người dùng.

Hệ thống hoạt động theo mô hình:
- Location Tracking (GPS)
- Geofencing Engine
- Narration Engine (Text-to-Speech)
- Offline-first Data Storage (SQLite)

---

## 2. Objectives

- Tự động phát thuyết minh không cần thao tác (hands-free)
- Hoạt động ổn định khi không có internet
- Tránh phát trùng bằng cơ chế cooldown
- Thiết kế theo kiến trúc tách lớp, dễ test và bảo trì

---

## 3. Technology Stack

| Layer | Technology |
|------|-----------|
| UI | .NET MAUI (XAML) |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| Map | Mapsui (OpenStreetMap) |
| Location | Geolocation API |
| Database | SQLite |
| Audio | Text-to-Speech |
| QR | ZXing.Net.MAUI |
| Backend | ASP.NET Core Web API |

---

## 4. System Architecture

### 4.1 High-level Architecture


[Mobile App]
├── View (UI)
├── ViewModel
├── Services
│ ├── LocationService
│ ├── GeofenceService
│ ├── NarrationService
│ └── SyncService
└── SQLite Database

[Backend API]
└── ASP.NET Core + SQL Server


---

### 4.2 Data Flow


GPS → LocationService
→ GeofenceService
→ NarrationService
→ SQLite (Log)
→ TTS Output


---

## 5. Core Features

### 5.1 Map & Location
- Hiển thị bản đồ bằng Mapsui (OpenStreetMap)
- Tracking GPS mỗi 3 giây
- Hiển thị POI và vị trí người dùng

### 5.2 Geofence Engine
- Tính khoảng cách bằng công thức Haversine
- Trigger khi user vào vùng POI
- Kiểm tra cooldown trước khi phát

### 5.3 Narration Engine
- Text-to-Speech đa ngôn ngữ (vi, en, zh)
- Hàng đợi audio (Queue)
- Không phát chồng âm thanh

### 5.4 QR Mode
- Quét QR để phát thuyết minh ngay
- Bỏ qua logic Geofence và Cooldown

### 5.5 Offline-first
- Toàn bộ dữ liệu lưu trong SQLite
- Sync khi có mạng

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

## 7. UML Diagrams

### 7.1 Sequence Diagram

```plantuml
@startuml
actor User
participant LocationService
participant GeofenceService
participant NarrationService
participant SQLite

User -> LocationService : Update Location
LocationService -> GeofenceService : Send Coordinates
GeofenceService -> SQLite : Check Cooldown
GeofenceService -> NarrationService : Trigger Audio
NarrationService -> SQLite : Save Log
NarrationService -> User : Play Audio
@enduml
7.2 Activity Diagram
@startuml
start
:Nhận GPS;
:Tính khoảng cách;
if (Trong bán kính?) then (Yes)
  if (Cooldown hợp lệ?) then (Yes)
    :Phát audio;
    :Lưu log;
  else (No)
    :Bỏ qua;
  endif
else (No)
  :Chờ cập nhật GPS;
endif
stop
@enduml
7.3 State Diagram
@startuml
[*] --> Idle
Idle --> Tracking
Tracking --> InRange
InRange --> Playing
Playing --> Cooldown
Cooldown --> Tracking
@enduml
7.4 Class Diagram
@startuml

class MapViewModel {
  +KhoiDongAsync()
}

class LocationService {
  +BatDauTracking()
}

class GeofenceService {
  +KiemTraGeofence()
}

class NarrationService {
  +PhatThuyetMinh()
}

class LocalDatabase {
  +LayPoi()
  +LuuLog()
}

MapViewModel --> LocationService
MapViewModel --> GeofenceService
GeofenceService --> NarrationService
GeofenceService --> LocalDatabase
NarrationService --> LocalDatabase

@enduml
8. Business Rules
Cooldown: 5 phút cho mỗi POI
QR override: bỏ qua cooldown
Queue audio: chỉ phát 1 audio tại 1 thời điểm
9. Constraints
iOS hạn chế background GPS
Không sử dụng Google Maps (không API key)
Không yêu cầu đăng nhập người dùng
10. Expected Outcome
App hoạt động ổn định ngoài thực tế
GPS tracking chính xác
Audio không bị lặp
Hoạt động offline hoàn chỉnh
Codebase dễ hiểu và dễ bảo trì
