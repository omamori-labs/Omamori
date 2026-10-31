using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Cake.Common;
using Cake.Common.Diagnostics;
using ProcessSettings = Cake.Core.IO.ProcessSettings;

namespace Omamori.Build
{
    internal static class LazerRelease
    {
        private const string repository = "ppy/osu";

        private const string game_files = "omamori-sandbox-files.txt";

        private static readonly HttpClient http = createClient();

        public static async Task SetupAsync(BuildContext context)
        {
            string? current = InstalledVersion(context.Sandbox);

            if (current == context.LazerVersion && !context.ForceSetup)
            {
                EnsurePortable(context.Sandbox);
                context.Information($"sandbox is already on {current}, nothing to do (--force-setup re-extracts it)");
                return;
            }

            context.Information(current == null ? $"setting up sandbox with lazer {context.LazerVersion}" : $"updating sandbox from {current} to {context.LazerVersion}");

            var asset = await findAssetAsync(context.LazerVersion, assetName(context.LazerVersion));
            string package = await downloadAsync(context, asset);

            Directory.CreateDirectory(context.Sandbox);
            removePreviousGameFiles(context);

            string[] extracted = OperatingSystem.IsWindows()
                ? extractNupkg(package, context.Sandbox)
                : extractAppImage(context, package, context.Sandbox);

            await File.WriteAllLinesAsync(Path.Combine(context.Sandbox, game_files), extracted);
            EnsurePortable(context.Sandbox);

            string? installed = InstalledVersion(context.Sandbox);
            if (installed != context.LazerVersion)
                throw new InvalidOperationException($"sandbox reports {installed ?? "no version"} after setup, expected {context.LazerVersion}");

            context.Information($"sandbox ready at {context.Sandbox}");
        }

        /// <summary>
        ///     osu-framework only keeps data beside the exe when framework.ini is there, without it the sandbox would use the real data folder
        /// </summary>
        public static void EnsurePortable(string sandbox)
        {
            string frameworkIni = PortableMarker(sandbox);
            if (!File.Exists(frameworkIni))
                File.WriteAllText(frameworkIni, string.Empty);
        }

        public static string PortableMarker(string sandbox) => Path.Combine(sandbox, "framework.ini");

        // the sandbox is also the data folder so it can't be wiped, only what the last setup extracted goes
        private static void removePreviousGameFiles(BuildContext context)
        {
            string list = Path.Combine(context.Sandbox, game_files);

            if (!File.Exists(list))
            {
                if (InstalledVersion(context.Sandbox) != null)
                    context.Warning($"no {game_files} from the last setup, files the old lazer had and this one doesn't will stay");

                return;
            }

            string root = Path.GetFullPath(context.Sandbox) + Path.DirectorySeparatorChar;

            foreach (string relative in File.ReadAllLines(list).Where(l => l.Length > 0))
            {
                string path = Path.GetFullPath(Path.Combine(root, relative));

                if (path.StartsWith(root, StringComparison.Ordinal) && File.Exists(path))
                    File.Delete(path);
            }
        }

        public static string? InstalledVersion(string sandbox)
        {
            string path = Path.Combine(sandbox, "sq.version");
            if (!File.Exists(path))
                return null;

            return XDocument.Load(path).Descendants().FirstOrDefault(e => e.Name.LocalName == "version")?.Value.Trim();
        }

        private static string assetName(string version)
        {
            if (OperatingSystem.IsWindows())
                return $"osulazer-{version}-full.nupkg";

            if (OperatingSystem.IsLinux())
                return $"osulazer-{version}-linux-x64-full.nupkg";

            throw new PlatformNotSupportedException("the sandbox can only be set up on windows and linux for now");
        }

        private static async Task<ReleaseAsset> findAssetAsync(string tag, string name)
        {
            using var response = await http.GetAsync(new Uri($"https://api.github.com/repos/{repository}/releases/tags/{tag}"));

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"couldn't read release {tag} from github ({(int)response.StatusCode}), set GITHUB_TOKEN if this is rate limiting");

            var release = await response.Content.ReadFromJsonAsync<Release>();

            return release?.Assets.FirstOrDefault(a => a.Name == name)
                   ?? throw new InvalidOperationException($"release {tag} has no {name}");
        }

        private static async Task<string> downloadAsync(BuildContext context, ReleaseAsset asset)
        {
            if (asset.Digest is not { } digest || !digest.StartsWith("sha256:", StringComparison.Ordinal))
                throw new InvalidOperationException($"{asset.Name} has no sha256 digest on github, refusing to use it unverified");

            string expected = digest["sha256:".Length..];
            string path = Path.Combine(context.Downloads, asset.Name);
            Directory.CreateDirectory(context.Downloads);

            if (File.Exists(path) && await sha256Async(path) == expected)
            {
                context.Information($"reusing verified {asset.Name}");
                return path;
            }

            context.Information($"downloading {asset.Name} ({asset.Size / 1024 / 1024} MB)");

            string partial = path + ".partial";

            await using (var download = await http.GetStreamAsync(asset.BrowserDownloadUrl))
            await using (var file = File.Create(partial))
                await download.CopyToAsync(file);

            string actual = await sha256Async(partial);

            if (actual != expected)
            {
                File.Delete(partial);
                throw new InvalidOperationException($"{asset.Name} sha256 is {actual}, github says {expected}, refusing to extract it");
            }

            File.Move(partial, path, overwrite: true);
            context.Information($"verified sha256 {expected}");
            return path;
        }

        private static string[] extractNupkg(string package, string target)
        {
            const string prefix = "lib/app/";
            string root = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
            var extracted = new List<string>();

            using var zip = ZipFile.OpenRead(package);

            foreach (var entry in zip.Entries.Where(e => e.FullName.StartsWith(prefix, StringComparison.Ordinal) && !e.FullName.EndsWith('/')))
            {
                string relative = Uri.UnescapeDataString(entry.FullName[prefix.Length..]);
                string destination = Path.GetFullPath(Path.Combine(root, relative));

                if (!destination.StartsWith(root, StringComparison.Ordinal))
                    throw new InvalidOperationException($"{entry.FullName} would extract outside the sandbox");

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, overwrite: true);
                extracted.Add(Path.GetRelativePath(root, destination));
            }

            return [.. extracted];
        }

        [UnsupportedOSPlatform("windows")]
        private static string[] extractAppImage(BuildContext context, string package, string target)
        {
            string work = Path.Combine(context.Downloads, "appimage");
            if (Directory.Exists(work))
                Directory.Delete(work, recursive: true);
            Directory.CreateDirectory(work);

            string appImage = Path.Combine(work, "osulazer.AppImage");

            using (var zip = ZipFile.OpenRead(package))
            {
                var entry = zip.Entries.SingleOrDefault(e => e.FullName.EndsWith(".AppImage", StringComparison.Ordinal))
                            ?? throw new InvalidOperationException("linux package has no AppImage");
                entry.ExtractToFile(appImage);
            }

            File.SetUnixFileMode(appImage, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            int exit = context.StartProcess(appImage, new ProcessSettings { Arguments = "--appimage-extract", WorkingDirectory = work, Silent = true });
            if (exit != 0)
                throw new InvalidOperationException($"--appimage-extract exited with {exit}");

            string game = Path.Combine(work, "squashfs-root", "usr", "bin");
            if (!File.Exists(Path.Combine(game, "osu.Game.dll")))
                throw new InvalidOperationException("the AppImage doesn't have the game in usr/bin/ anymore, the layout changed");

            string[] copied = copyDirectory(game, target);
            Directory.Delete(work, recursive: true);
            return copied;
        }

        private static string[] copyDirectory(string source, string target)
        {
            foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));

            var copied = new List<string>();

            // File.Copy keeps the unix mode, so osu! stays executable
            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(source, file);
                File.Copy(file, Path.Combine(target, relative), overwrite: true);
                copied.Add(relative);
            }

            return [.. copied];
        }

        private static async Task<string> sha256Async(string path)
        {
            await using var file = File.OpenRead(path);
            return Convert.ToHexStringLower(await SHA256.HashDataAsync(file));
        }

        private static HttpClient createClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("omamori-build", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            // ci passes its token to avoid the 60 requests an hour anonymous limit
            if (Environment.GetEnvironmentVariable("GITHUB_TOKEN") is { Length: > 0 } token)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        private sealed record Release([property: JsonPropertyName("assets")] ReleaseAsset[] Assets);

        private sealed record ReleaseAsset(
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("size")] long Size,
            [property: JsonPropertyName("digest")] string? Digest,
            [property: JsonPropertyName("browser_download_url")] Uri BrowserDownloadUrl);
    }
}
