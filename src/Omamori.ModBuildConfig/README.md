# Omamori.ModBuildConfig

Build setup for [Omamori](https://github.com/omamori-labs/Omamori) mods for osu!lazer

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Omamori.ModBuildConfig"          Version="x.y.z" />
  </ItemGroup>
</Project>
```

Put a `mod.json` next to the project.

| Property | Default | |
|---|---|---|
| `OmamoriGamePath` | Lazer path | the folder containing `osu.Game.dll` |
| `OmamoriDataPath` | osu!'s data folder | where `omamori/mods/` is |
| `OmamoriDeploy` | `false` | `true` copies the mod into `$(OmamoriDataPath)/omamori/mods/<id>/` after each build |
| `OmamoriPackMod` | `true` for Release | zips the mod as `<id>-<version>.zip` to extract into `omamori/mods/` |
