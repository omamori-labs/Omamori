using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Frosting;
using ProcessSettings = Cake.Core.IO.ProcessSettings;

namespace Omamori.Build
{
    [TaskName("ModBuildCheck")]
    [TaskDescription("make a mod from the packed template and build it against the packed Omamori.ModBuildConfig")]
    [IsDependentOn(typeof(PackTask))]
    public sealed class ModBuildCheckTask : FrostingTask<BuildContext>
    {
        private const string mod_name = "BuildCheck";
        private const string mod_id = "yourname.buildcheck";

        public override void Run(BuildContext context)
        {
            var templates = new DirectoryInfo(context.Packages).GetFiles("Omamori.Templates.*.nupkg")
                                                               .Where(f => !f.Name.EndsWith(".symbols.nupkg", StringComparison.Ordinal))
                                                               .MaxBy(f => f.LastWriteTimeUtc)
                            ?? throw new InvalidOperationException($"no Omamori.Templates package in {context.Packages}, run Pack first");

            string root = Path.Combine(Path.GetTempPath(), $"omamori-buildcheck-{Guid.NewGuid():N}");
            string hive = Path.Combine(root, "hive");
            string project = Path.Combine(root, mod_name);

            try
            {
                Directory.CreateDirectory(root);

                dotnet(context, root, $"new install \"{templates.FullName}\" --debug:custom-hive \"{hive}\"");
                dotnet(context, root, $"new omamori-mod -n {mod_name} -o \"{project}\" --debug:custom-hive \"{hive}\"");

                addCheckFiles(context, root, project);

                context.DotNetBuild(Path.Combine(project, $"{mod_name}.csproj"), new DotNetBuildSettings
                {
                    Configuration = "Release",
                    NoLogo = true,
                    MSBuildSettings = new DotNetMSBuildSettings().WithProperty("OmamoriGamePath", context.Sandbox).WithProperty("OmamoriDataPath", Path.Combine(root, "data")),
                });

                checkManifest(Path.Combine(project, "mod.json"));
                checkZip(Path.Combine(project, "bin", "Release", $"{mod_id}-1.0.0.zip"));
                context.Information($"{templates.Name} makes a mod that builds into a complete zip");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        private static void dotnet(BuildContext context, string workingDirectory, string arguments)
        {
            int exit = context.StartProcess("dotnet", new ProcessSettings { Arguments = arguments, WorkingDirectory = workingDirectory });

            if (exit != 0)
                throw new InvalidOperationException($"dotnet {arguments} exited with {exit}");
        }

        private static void addCheckFiles(BuildContext context, string root, string project)
        {
            File.WriteAllText(Path.Combine(root, "nuget.config"), $"""
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

            File.WriteAllText(Path.Combine(root, "Directory.Build.targets"), $"""
                <Project>
                  <PropertyGroup>
                    <RestorePackagesPath>{Path.Combine(root, "packages")}</RestorePackagesPath>
                  </PropertyGroup>
                  <ItemGroup>
                    <None Update="assets\note.txt" CopyToOutputDirectory="PreserveNewest" />
                  </ItemGroup>
                </Project>
                """);

            Directory.CreateDirectory(Path.Combine(project, "assets"));
            File.WriteAllText(Path.Combine(project, "assets", "note.txt"), "a content file in a subfolder");
        }

        private static void checkManifest(string path)
        {
            using var manifest = JsonDocument.Parse(File.ReadAllText(path));
            string? id = manifest.RootElement.GetProperty("id").GetString();

            if (id != mod_id)
                throw new InvalidOperationException($"the template gave the mod id \"{id}\", expected \"{mod_id}\" from the project name");
        }

        private static void checkZip(string path)
        {
            if (!File.Exists(path))
                throw new InvalidOperationException($"the mod build didn't make {path}");

            using var zip = ZipFile.OpenRead(path);
            var entries = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToHashSet();

            string[] expected = [$"{mod_id}/{mod_name}.dll", $"{mod_id}/mod.json", $"{mod_id}/{mod_name}.deps.json", $"{mod_id}/assets/note.txt"];
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
