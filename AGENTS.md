# AGENTS.md - DoAnCsharp

## Goal

This repository contains a .NET MAUI 10 mobile app and an ASP.NET Core 10 API written in C#.

When modifying code, the agent must:

- Keep the project buildable.
- Preserve the existing architecture.
- Ensure the app runs on a real Android device.
- Deliver working features (not just compiling code).

---

## Repo Map

- `App/App/App.csproj`: .NET MAUI mobile app
- `App/App/Models`: data models (POI, history, settings)
- `App/App/ViewModels`: UI logic and commands
- `App/App/Views`: XAML UI, minimal code-behind
- `App/App/Services`: GPS, SQLite, API sync, TTS, QR, navigation
- `App/App/Resources/Raw/map`: local Leaflet assets (no CDN)

- `VinhKhanhApi/VinhKhanhApi.csproj`: ASP.NET Core API
- `VinhKhanhApi/Controllers`: API endpoints
- `VinhKhanhApi/Data`, `Models`, `Services`, `Migrations`: backend logic and database

Do NOT modify generated files unless explicitly required:

- `.codex-build/`
- `bin/`
- `obj/`
- `*.csproj.user`

---

## Platform Rules

Do NOT downgrade:

- Do not change to `net9.0`
- Do not lower SDK version

Always keep:

- App: `net10.0-android`
- API: `net10.0`

Do NOT:

- Change target framework
- Change project structure
- Add new dependencies without strong reason
- Delete existing code without understanding its flow

---

## Windows Shell Notes

- Prefer `rg` for searching when available
- If `rg` fails with "Access Denied" or cannot execute:
  - Use PowerShell:
    - `Get-ChildItem -Recurse -File`
    - `Select-String`
    - `Get-Content`
- Do NOT retry `rg` repeatedly after it fails once

---

## Architecture (MAUI)

The app follows MVVM:

| Layer | Responsibility |
|------|--------------|
| Models | Data |
| ViewModels | UI logic |
| Views | UI only |
| Services | API, DB, GPS, TTS, QR |

Rules:

- No business logic in `.xaml.cs`
- No direct DB access in Views
- Logic must be in ViewModel or Service
- Code-behind only for UI lifecycle or minimal events

---

## Editing Rules

- Make minimal changes only
- Do NOT refactor large areas unless explicitly required
- Do NOT rename files/classes/methods unless necessary

When fixing features:

1. Read before editing
2. Identify current flow and related files
3. Modify only what is needed
4. Avoid wide refactors
5. Remove dead code if clearly obsolete
6. Use async/await correctly
7. Avoid UI blocking
8. Avoid infinite loops or API spam

---

## Android Device Requirement

All features must work on a real Android device.

Do NOT:

- Hardcode `localhost`
- Only support emulator (`10.0.2.2`)
- Depend on local dev machine

Must:

- Support configurable API base URL (LAN IP / public / QR)
- Work offline using SQLite or sample data
- Handle network errors, timeout, invalid JSON

---

## Data & API Rules

Do NOT assume API is always available.

POI loading must support:

- Online → sync API → save SQLite
- Offline → load SQLite or sample data

App must NOT crash if:

- API returns invalid JSON
- Timeout occurs
- HTTP error occurs

If modifying API:

- Do NOT expose real secrets
- Do NOT hardcode production connection string
- Keep backward compatibility with app

---

## Map Rules

- Do NOT use Google Maps
- Use Leaflet in WebView
- All assets must be local (`Resources/Raw/map`)
- No CDN usage

Must still work if API fails

---

## GPS / TTS / QR Rules

GPS:

- Handle permission denied
- Handle null location
- Do NOT crash

TTS:

- Avoid overlapping audio
- Support stop/cancel
- Handle missing voice

QR:

- Handle no camera permission
- Handle invalid QR
- Do NOT crash

---

## Build & Verification

### Build App (MAUI)

```powershell
dotnet restore App/App/App.csproj
dotnet build App/App/App.csproj --framework net10.0-android --configuration Debug
Build API
dotnet restore VinhKhanhApi/VinhKhanhApi.csproj
dotnet build VinhKhanhApi/VinhKhanhApi.csproj --configuration Debug

Run both if both are modified.

Build Lock Issues

If build fails due to file lock (bin/ or obj/):

Check if app/API is running
Stop only related processes
Retry the same build command
Do NOT delete source files
Done Criteria

Task is complete ONLY if:

Build succeeds
No compile errors
No new issues introduced
Logic matches requirements
Changes are minimal
Required Report

After finishing, the agent must report:

Files modified
Files added
Files deleted
Framework built
Build result
Remaining risks (if any)