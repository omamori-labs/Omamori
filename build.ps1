#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
dotnet run --project "$PSScriptRoot/build/Build.csproj" -- @args
exit $LASTEXITCODE
