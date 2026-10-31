using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Omamori.Build
{
    [TaskName("Smoke")]
    [TaskDescription("launch the sandbox, print omamori's log lines, then close it again, --faults is optional")]
    [IsDependentOn(typeof(DeployTask))]
    public sealed partial class SmokeTask : AsyncFrostingTask<BuildContext>
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(90);

        public override async Task RunAsync(BuildContext context)
        {
            if (runningLazer() is string path)
                throw new InvalidOperationException($"lazer is already running ({path}), close it first");

            if (!File.Exists(LazerRelease.PortableMarker(context.Sandbox)))
                throw new InvalidOperationException($"no framework.ini in {context.Sandbox}, the game would use the real osu! data folder, run SetupSandbox first");

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

                string[] lines = readShared(log);

                foreach (string line in lines.Where(l => l.Contains("[Omamori]", StringComparison.Ordinal)))
                    context.Information(line.TrimEnd('\r'));

                int unhandled = lines.Count(l => l.Contains("An unhandled error", StringComparison.Ordinal));
                if (unhandled > 0)
                    context.Warning($"{unhandled} unhandled error(s) in {log}");
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

        [GeneratedRegex(@"Loaded \d+ mod|[Rr]unning without mods|Created mods directory|Features: \d+/\d+")]
        private static partial Regex done();
    }
}
