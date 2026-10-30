#!/usr/bin/env bash
set -euo pipefail
dotnet run --project "$(dirname "$0")/build/Build.csproj" -- "$@"
