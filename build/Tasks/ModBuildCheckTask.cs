using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("ModBuildCheck")]
    [TaskDescription("build a throwaway mod against the packed Omamori.ModBuildConfig")]
    [IsDependentOn(typeof(PackTask))]
    public sealed class ModBuildCheckTask : FrostingTask<BuildContext>
    {
        private const string mod_id = "omamori.buildcheck";

        public override void Run(BuildContext context)
        {
            var package = new DirectoryInfo(context.Packages).GetFiles("Omamori.ModBuildConfig.*.nupkg")
                                                             .Where(f => !f.Name.EndsWith(".symbols.nupkg", StringComparison.Ordinal))
                                                             .MaxBy(f => f.LastWriteTimeUtc)
                          ?? throw new InvalidOperationException($"no Omamori.ModBuildConfig package in {context.Packages}, run Pack first");

            string version = Path.GetFileNameWithoutExtension(package.Name)["Omamori.ModBuildConfig.".Length..];

            string project = Path.Combine(Path.GetTempPath(), $"omamori-buildcheck-{Guid.NewGuid():N}");

            try
            {
                writeMod(context, project, version);

                context.DotNetBuild(Path.Combine(project, "BuildCheck.csproj"), new DotNetBuildSettings
                {
                    Configuration = "Release",
                    NoLogo = true,
                    MSBuildSettings = new DotNetMSBuildSettings().WithProperty("OmamoriGamePath", context.Sandbox),
                });

                checkZip(Path.Combine(project, "bin", "Release", $"{mod_id}-1.0.0.zip"));
                context.Information($"Omamori.ModBuildConfig {version} builds a complete mod zip");
            }
            finally
            {
                if (Directory.Exists(project))
                    Directory.Delete(project, recursive: true);
            }
        }

        private static void writeMod(BuildContext context, string project, string version)
        {
            Directory.CreateDirectory(Path.Combine(project, "assets"));

            string apiVersion = Regex.Match(File.ReadAllText(Path.Combine(context.Root, "src", "Omamori.API", "ApiInfo.cs")), "VERSION = \"([^\"]+)\"").Groups[1].Value;

            File.WriteAllText(Path.Combine(project, "nuget.config"), $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="local" value="{context.Packages}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                  </packageSources>
                  <packageSourceMapping>
                    <packageSource key="local">
                      <package pattern="Omamori.*" />
                    </packageSource>
                    <packageSource key="nuget.org">
                      <package pattern="*" />
                    </packageSource>
                  </packageSourceMapping>
                </configuration>
                """);

            File.WriteAllText(Path.Combine(project, "BuildCheck.csproj"), $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <RestorePackagesPath>{Path.Combine(project, "packages")}</RestorePackagesPath>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Omamori.ModBuildConfig" Version="{version}" />
                    <None Include="assets\note.txt" CopyToOutputDirectory="PreserveNewest" />
                  </ItemGroup>
                </Project>
                """);

            File.WriteAllText(Path.Combine(project, "mod.json"), $$"""
                {"id":"{{mod_id}}","name":"Build check","version":"1.0.0","entry":"BuildCheck.dll","apiVersion":"{{apiVersion}}","capabilities":["Cosmetic"]}
                """);

            File.WriteAllText(Path.Combine(project, "Nothing.cs"), "namespace BuildCheck { public static class Nothing { } }");
            File.WriteAllText(Path.Combine(project, "assets", "note.txt"), "a content file in a subfolder");
        }

        private static void checkZip(string path)
        {
            if (!File.Exists(path))
                throw new InvalidOperationException($"the mod build didn't make {path}");

            using var zip = ZipFile.OpenRead(path);
            var entries = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToHashSet();

            string[] expected = [$"{mod_id}/BuildCheck.dll", $"{mod_id}/mod.json", $"{mod_id}/BuildCheck.deps.json", $"{mod_id}/assets/note.txt"];
            string[] bundled = [.. entries.Where(e => e.EndsWith("/osu.Game.dll", StringComparison.Ordinal) || e.EndsWith("/Omamori.API.dll", StringComparison.Ordinal))];

            var missing = expected.Where(e => !entries.Contains(e)).ToList();

            if (missing.Count > 0 || bundled.Length > 0)
            {
                throw new InvalidOperationException($"mod zip is wrong, missing: [{string.Join(", ", missing)}], bundled game or api: [{string.Join(", ", bundled)}], "
                                                    + $"has: [{string.Join(", ", entries.Order(StringComparer.Ordinal))}]");
            }
        }
    }
}
