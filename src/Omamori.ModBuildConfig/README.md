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

Put a `mod.json` next to the project. Build with the dotnet cli, Visual Studio's msbuild skips checking `mod.json`, deploying and zipping.

| Property | Default | |
|---|---|---|
| `OmamoriGamePath` | where osu!lazer is installed | the folder containing `osu.Game.dll` |
| `OmamoriDataPath` | osu!'s data folder | where `omamori/mods/` is |
| `OmamoriDeploy` | `false` | `true` copies the mod into `$(OmamoriDataPath)/omamori/mods/<id>/` after each build, replacing what's there |
| `OmamoriPackMod` | `true` for Release | zips the mod as `<id>-<version>.zip` to extract into `omamori/mods/` |
| `OmamoriHarmony` | `false` | `true` lets the mod patch osu! with Harmony, see below |

Deploy and the zip take everything in the build output, including content files, `runtimes/` and translated resources.

## Using another mod's services

Declare the other mod in `mod.json`'s `dependencies`, then reference its dll without bundling it, so both mods share one copy of its types:

```xml
<ItemGroup>
  <OmamoriModReference Include="$(OmamoriDataPath)\omamori\mods\yourname.othermod\OtherMod.dll" />
</ItemGroup>
```

## Patching osu!

When the API doesn't cover something, a mod can patch the game with [Harmony](https://harmony.pardeike.net). Omamori loads one Harmony for every mod, so don't add the `Lib.Harmony` package. Set `OmamoriHarmony` instead, which compiles against Omamori's copy and never bundles it:

```xml
<PropertyGroup>
  <OmamoriHarmony>true</OmamoriHarmony>
</PropertyGroup>
```

`dotnet new omamori-mod --harmony` starts a mod with this set and an example patch.

Apply patches from `Load` with your mod id as the Harmony id. If your mod crashes, Omamori removes its patches with the rest of it:

```csharp
new Harmony(context.Manifest.Id).CreateClassProcessor(typeof(MyPatch)).Patch();
```

To reach lazer's private members, publicize them with [Krafs.Publicizer](https://github.com/krafs/Publicizer) instead of using reflection, so a rename in an osu! update breaks your build rather than your mod:

```xml
<PropertyGroup>
  <PublicizerRuntimeStrategies>IgnoresAccessChecksTo</PublicizerRuntimeStrategies>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Krafs.Publicizer" Version="2.3.2" PrivateAssets="all" />
  <Publicize Include="osu.Game:osu.Game.Screens.Menu.IntroScreen.LoadMenu" />
</ItemGroup>
```

## Libraries osu! already has

Any package that osu!lazer also ships is compiled against the game's copy and never bundled, since only one copy can load. To keep your own copy of one anyway, for example a test framework in a test project:

```xml
<ItemGroup>
  <OmamoriOwnCopy Include="nunit.framework" />
</ItemGroup>
```
