using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>the lazer release everything builds against, from lazer-version.txt</summary>
        public string LazerVersion { get; }

        /// <summary>--sandbox overrides where the game goes, builds then compile against it too</summary>
        public string Sandbox { get; }

        public string Stage => Path.Combine(Root, "artifacts", "stage", BuildConfiguration);

        public string Downloads => Path.Combine(Root, "artifacts", "downloads");

        public string TestResults => Path.Combine(Root, "artifacts", "test-results");

        public string Solution => Path.Combine(Root, "Omamori.sln");

        public DotNetMSBuildSettings MSBuild => new DotNetMSBuildSettings().WithProperty("LazerPath", Sandbox);

        public string GameExecutable => Path.Combine(Sandbox, OperatingSystem.IsWindows() ? "osu!.exe" : "osu!");

        public bool ForceSetup { get; }

        public ICollection<string> ModSources { get; }

        public string Faults { get; }

        public string? Screenshot { get; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            Root = findRoot();
            BuildConfiguration = context.Argument("configuration", "Release");
            Sandbox = Path.GetFullPath(context.Argument("sandbox", Path.Combine(Root, "sandbox", "lazer")));
            LazerVersion = File.ReadAllText(Path.Combine(Root, "lazer-version.txt")).Trim();
            ForceSetup = context.HasArgument("force-setup");
            ModSources = context.Arguments<string>("mods", [Path.Combine(Root, "examples")]);
            Faults = context.Argument("faults", string.Empty);
            Screenshot = context.HasArgument("screenshot") ? Path.GetFullPath(context.Argument<string>("screenshot")) : null;
        }

        private static string findRoot()
        {
            for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "Omamori.sln")))
                    return dir.FullName;
            }

            throw new InvalidOperationException("run the build from inside the repo, Omamori.sln wasn't found above the working directory");
        }
    }
}
