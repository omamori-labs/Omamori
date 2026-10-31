# Omamori - An Osu!Lazer Modding Framework

WIP

## Building

Required:

   1. .NET 10 SDK
   2. osu!lazer install

Steps:

1. Copy the installed game into sandbox/
   ```powershell
   Copy-Item "$env:LOCALAPPDATA\osulazer\current" sandbox\lazer -Recurse
   ```
2. Make the copy portable
   ```powershell
   New-Item sandbox\lazer\framework.ini -ItemType File
   ```
3. Build:
   ```powershell
   dotnet build Omamori.slnx -c Release
   ```

When osu updates delete `sandbox/lazer`, repeat and rebuld

To build against a different copy, pass `-p:LazerPath=<path>`.