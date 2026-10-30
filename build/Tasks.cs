using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Test;
using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("SetupSandbox")]
    [TaskDescription("download, verify and extract the lazer release from lazer-version.txt into sandbox/lazer")]
    public sealed class SetupSandboxTask : AsyncFrostingTask<BuildContext>
    {
        public override Task RunAsync(BuildContext context) => LazerRelease.SetupAsync(context);
    }

    [TaskName("Build")]
    [TaskDescription("build the solution, which also stages artifacts/stage/<configuration>")]
    [IsDependentOn(typeof(SetupSandboxTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context) =>
            context.DotNetBuild(context.Solution, new DotNetBuildSettings { Configuration = context.BuildConfiguration, NoLogo = true, MSBuildSettings = context.MSBuild });
    }

    [TaskName("Test")]
    [TaskDescription("run the test suite, results go to artifacts/test-results")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class TestTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context) =>
            context.DotNetTest(context.Solution, new DotNetTestSettings
            {
                Configuration = context.BuildConfiguration,
                NoBuild = true,
                NoLogo = true,
                ResultsDirectory = context.TestResults,
                MSBuildSettings = context.MSBuild,
                Loggers = ["trx"],
            });
    }

    [TaskName("Deploy")]
    [TaskDescription("install the staged build and the mods from --mods (default examples/) into the sandbox")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class DeployTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            string rulesets = Path.Combine(context.Sandbox, "rulesets");
            Directory.CreateDirectory(rulesets);
            foreach (string file in Directory.EnumerateFiles(Path.Combine(context.Stage, "rulesets")))
                File.Copy(file, Path.Combine(rulesets, Path.GetFileName(file)), overwrite: true);

            mirror(Path.Combine(context.Stage, "omamori", "core"), Path.Combine(context.Sandbox, "omamori", "core"));

            foreach (string source in context.ModSources.Where(Directory.Exists))
            {
                foreach (string project in Directory.EnumerateFiles(source, "*.csproj", SearchOption.AllDirectories))
                    deployMod(context, project);
            }

            context.Information($"deployed to {context.Sandbox}");
        }

        private static void deployMod(BuildContext context, string project)
        {
            context.DotNetBuild(project, new DotNetBuildSettings { Configuration = context.BuildConfiguration, NoLogo = true, MSBuildSettings = context.MSBuild });

            string name = Path.GetFileNameWithoutExtension(project);
            string output = Path.Combine(Path.GetDirectoryName(project)!, "bin", context.BuildConfiguration, "net10.0");
            string target = Path.Combine(context.Sandbox, "omamori", "mods", name.ToLowerInvariant());

            mirror(output, target, f => Path.GetFileName(f) == "mod.json" || f.EndsWith(".dll", StringComparison.Ordinal)
                                        || f.EndsWith(".pdb", StringComparison.Ordinal) || f.EndsWith(".deps.json", StringComparison.Ordinal));

            context.Information($"  mod {name}");
        }

        private static void mirror(string source, string target, Func<string, bool>? include = null)
        {
            if (Directory.Exists(target))
                Directory.Delete(target, recursive: true);
            Directory.CreateDirectory(target);

            foreach (string file in Directory.EnumerateFiles(source).Where(f => include?.Invoke(f) ?? true))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }
    }

    [TaskName("Smoke")]
    [TaskDescription("launch the sandbox, print omamori's log lines, then close it again, --faults and --screenshot <png> are optional")]
    [IsDependentOn(typeof(DeployTask))]
    public sealed partial class SmokeTask : AsyncFrostingTask<BuildContext>
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(90);

        public override async Task RunAsync(BuildContext context)
        {

            if (runningLazer() is string path)
                throw new InvalidOperationException($"lazer is already running ({path}), close it first");

            var start = DateTime.Now;
            var info = new ProcessStartInfo(context.GameExecutable) { WorkingDirectory = context.Sandbox, UseShellExecute = false };
            info.Environment["OSU_DISABLE_ERROR_REPORTING"] = "1";
            if (context.Faults.Length > 0)
                info.Environment["OMAMORI_FAULTS"] = context.Faults;

            using var game = Process.Start(info) ?? throw new InvalidOperationException("couldn't start the sandbox");

            try
            {
                string log = await waitForOmamoriAsync(context, game, start);

                await Task.Delay(TimeSpan.FromSeconds(6));

                foreach (string line in File.ReadLines(log).Where(l => l.Contains("[Omamori]", StringComparison.Ordinal)))
                    context.Information(line);

                int unhandled = File.ReadLines(log).Count(l => l.Contains("An unhandled error", StringComparison.Ordinal));
                if (unhandled > 0)
                    context.Warning($"{unhandled} unhandled error(s) in {log}");

                if (context.Screenshot != null)
                {
                    if (!OperatingSystem.IsWindows())
                        throw new PlatformNotSupportedException("--screenshot is windows only");

                    await WindowCapture.CaptureAsync(game, context.Screenshot);
                    context.Information($"screenshot saved to {context.Screenshot}");
                }
            }
            finally
            {
                if (!game.HasExited)
                {
                    game.Kill();
                    await game.WaitForExitAsync();
                }
            }
        }

        private static async Task<string> waitForOmamoriAsync(BuildContext context, Process game, DateTime start)
        {
            string logs = Path.Combine(context.Sandbox, "logs");
            var deadline = DateTime.Now + timeout;

            while (DateTime.Now < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));

                if (game.HasExited)
                    throw new InvalidOperationException($"game exited early with code {game.ExitCode}");

                var log = Directory.Exists(logs)
                    ? new DirectoryInfo(logs).GetFiles("*runtime*.log").Where(f => f.LastWriteTime > start).MaxBy(f => f.LastWriteTime)
                    : null;

                // FileShare.ReadWrite, the game still has it open
                if (log != null && readShared(log.FullName).Any(l => done().IsMatch(l)))
                    return log.FullName;
            }

            throw new TimeoutException($"omamori didn't finish loading within {timeout.TotalSeconds}s");
        }

        private static string? runningLazer()
        {
            foreach (var process in Process.GetProcessesByName("osu!"))
            {
                using (process)
                {
                    string? path;

                    try
                    {
                        path = process.MainModule?.FileName;
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        return $"pid {process.Id}";
                    }

                    if (path != null && File.Exists(Path.Combine(Path.GetDirectoryName(path)!, "osu.Game.dll")))
                        return path;
                }
            }

            return null;
        }

        private static string[] readShared(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Split('\n');
        }

        [GeneratedRegex(@"Loaded \d+ mod|running without mods|Created mods directory|Features: \d+/\d+")]
        private static partial Regex done();
    }

    [TaskName("CI")]
    [TaskDescription("what ci runs: setup, build, test")]
    [IsDependentOn(typeof(TestTask))]
    public sealed class CiTask : FrostingTask<BuildContext>
    {
    }

    [TaskName("Default")]
    [IsDependentOn(typeof(TestTask))]
    public sealed class DefaultTask : FrostingTask<BuildContext>
    {
    }
}
