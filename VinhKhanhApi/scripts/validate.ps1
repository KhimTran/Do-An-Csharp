param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "..\VinhKhanhApi.csproj"

Write-Host "==> Restore"
dotnet restore $project

Write-Host "==> Build (warnings as errors)"
dotnet build $project --configuration $Configuration --no-restore -warnaserror

Write-Host "==> Test"
dotnet test $project --configuration $Configuration --no-build --verbosity normal

Write-Host "✅ Validation passed."
