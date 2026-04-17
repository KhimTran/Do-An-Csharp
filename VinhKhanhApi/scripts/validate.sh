#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/VinhKhanhApi.csproj"

echo "==> Restore"
dotnet restore "$PROJECT"

echo "==> Build (warnings as errors)"
dotnet build "$PROJECT" --configuration Release --no-restore -warnaserror

echo "==> Test"
dotnet test "$PROJECT" --configuration Release --no-build --verbosity normal

echo "✅ Validation passed."
