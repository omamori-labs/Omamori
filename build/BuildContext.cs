using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cake.Common;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Core;
using Cake.Frosting;

namespace Omamori.Build
{
    public sealed class BuildContext : FrostingContext
    {
        /// <summary>the repo root</summary>
        public string Root { get; }

        public string BuildConfiguration { get; }

        /// <summary>the lazer release everything builds against, from build/lazer-version.txt</summary>
        public string LazerVersion { get; }

        /// <summary>--sandbox overrides where the game goes, builds then compile against it too</summary>
        public string Sandbox { get; }

        public string Stage => Path.Combine(Root, "artifacts", "stage", BuildConfiguration);

        public string Downloads => Path.Combine(Root, "artifacts", "downloads");

        public string TestResults => Path.Combine(Root, "artifacts", "test-results");

        public string Packages => Path.Combine(Root, "artifacts", "packages");

        public string Solution => Path.Combine(Root, "Omamori.slnx");

        public DotNetMSBuildSettings MSBuild => new DotNetMSBuildSettings().WithProperty("LazerPath", Sandbox);

        public string GameExecutable => Path.Combine(Sandbox, OperatingSystem.IsWindows() ? "osu!.exe" : "osu!");

        public bool ForceSetup { get; }

        public ICollection<string> ModSources { get; }

        public string Faults { get; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            Root = findRoot();
            BuildConfiguration = context.Argument("configuration", "Release");
            Sandbox = Path.GetFullPath(context.Argument("sandbox", Path.Combine(Root, "sandbox", "lazer")));

            if (realOsuFolders().FirstOrDefault(f => isInside(Sandbox, f)) is { } real)
                throw new InvalidOperationException($"--sandbox {Sandbox} is inside the real osu! folder {real}, point it somewhere of its own");

            LazerVersion = File.ReadAllText(Path.Combine(Root, "build", "lazer-version.txt")).Trim();
            ForceSetup = context.HasArgument("force-setup");
            ModSources = context.Arguments<string>("mods", []);
            Faults = context.Argument("faults", string.Empty);
        }

        private static IEnumerable<string> realOsuFolders()
        {
            string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

            string[] data =
            [
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "osu"),
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "osu"),
                Path.Combine(home, "Library", "Application Support", "osu"),
            ];

            foreach (string folder in data)
            {
                yield return folder;

                string storageIni = Path.Combine(folder, "storage.ini");

                if (File.Exists(storageIni)
                    && Regex.Match(File.ReadAllText(storageIni), @"FullPath\s*=\s*(.+)") is { Success: true } moved
                    && moved.Groups[1].Value.Trim() is { Length: > 0 } path)
                    yield return path;
            }

            yield return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "osulazer");
            yield return Path.Combine("/Applications", "osu!.app");
        }

        private static bool isInside(string path, string folder)
        {
            var comparison = OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));

            return path.Equals(full, comparison) || path.StartsWith(full + Path.DirectorySeparatorChar, comparison);
        }

        private static string findRoot()
        {
            for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Omamori.slnx")))
                    return dir.FullName;
            }

            throw new InvalidOperationException("run the build from inside the repo, Omamori.slnx wasn't found above the working directory");
        }
    }
}
