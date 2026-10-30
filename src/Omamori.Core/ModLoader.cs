using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Omamori.API;

namespace Omamori.Core
{
    /// <summary>
    ///     discover -> validate -> required hooks -> dependencies -> load order -> load
    /// </summary>
    internal static class ModLoader
    {
        public const string MANIFEST_FILENAME = "mod.json";

        private static readonly JsonSerializerOptions json_options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
        };

        public static readonly List<LoadedMod> LOADED = [];

        public static bool IsLoaded(string id) => LOADED.Any(m => m.Manifest.Id == id);

        public static LoadedMod? Get(string id) => LOADED.FirstOrDefault(m => m.Manifest.Id == id);

        public static void LoadAll(string modsDirectory)
        {
            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                LoaderLog.Info($"Created mods directory at {modsDirectory}.");
                return;
            }

            var skipped = new List<SkippedMod>();

            var candidates = discover(modsDirectory, skipped);
            candidates = filter(candidates, skipped, validate);
            candidates = dedupe(candidates, skipped);
            candidates = filter(candidates, skipped, checkRequiredHooks);
            candidates = resolveDependencies(candidates, skipped);

            foreach (var candidate in order(candidates, skipped))
            {
                try
                {
                    string? problem = checkDependenciesLoaded(candidate) ?? load(candidate);

                    if (problem != null)
                        skipped.Add(new SkippedMod(candidate.Label, problem));
                }
                catch (Exception e)
                {
                    LoaderLog.Info(e.ToString());
                    skipped.Add(new SkippedMod(candidate.Label, $"failed to load: {(e as TargetInvocationException)?.InnerException?.Message ?? e.Message}"));
                }
            }

            report(skipped);
        }

        private static List<ModCandidate> discover(string modsDirectory, List<SkippedMod> skipped)
        {
            var candidates = new List<ModCandidate>();

            foreach (string directory in Directory.GetDirectories(modsDirectory).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                string folder = Path.GetFileName(directory);
                string manifestPath = Path.Combine(directory, MANIFEST_FILENAME);

                if (!File.Exists(manifestPath))
                {
                    LoaderLog.Info($"Skipping {folder}: no {MANIFEST_FILENAME}.");
                    continue;
                }

                try
                {
                    var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), json_options)
                                   ?? throw new InvalidDataException("manifest is empty");

                    normalize(manifest);
                    candidates.Add(new ModCandidate(directory, manifest));
                }
                catch (Exception e)
                {
                    skipped.Add(new SkippedMod(folder, $"unreadable {MANIFEST_FILENAME}: {e.Message}"));
                }
            }

            return candidates;
        }

        private static void normalize(ModManifest manifest)
        {
            manifest.Id = manifest.Id?.Trim() ?? string.Empty;
            manifest.Name = manifest.Name?.Trim() ?? string.Empty;
            manifest.Version = manifest.Version?.Trim() ?? string.Empty;
            manifest.Author = manifest.Author?.Trim() ?? string.Empty;
            manifest.Description = manifest.Description?.Trim() ?? string.Empty;
            manifest.Entry = manifest.Entry?.Trim() ?? string.Empty;
            manifest.ApiVersion = manifest.ApiVersion?.Trim() ?? string.Empty;
            manifest.Capabilities ??= [];
            manifest.Hooks ??= [];
            manifest.Dependencies = [.. (manifest.Dependencies ?? []).Where(d => d != null)];

            foreach (var dependency in manifest.Dependencies)
            {
                dependency.Id = dependency.Id?.Trim() ?? string.Empty;
                dependency.MinVersion = dependency.MinVersion?.Trim() ?? string.Empty;
            }
        }

        private static List<ModCandidate> filter(List<ModCandidate> candidates, List<SkippedMod> skipped, Func<ModCandidate, string?> check)
        {
            var passed = new List<ModCandidate>();

            foreach (var candidate in candidates)
            {
                string? problem;

                try
                {
                    problem = check(candidate);
                }
                catch (Exception e)
                {
                    LoaderLog.Info(e.ToString());
                    problem = $"invalid manifest: {e.Message}";
                }

                if (problem == null)
                    passed.Add(candidate);
                else
                    skipped.Add(new SkippedMod(candidate.Label, problem));
            }

            return passed;
        }

        private static string? validate(ModCandidate candidate)
        {
            var manifest = candidate.Manifest;

            if (string.IsNullOrWhiteSpace(manifest.Id))
                return "manifest has no id";

            if (!ApiVersions.IsCompatible(manifest.ApiVersion, out string? apiProblem))
                return apiProblem;

            if (!ModVersions.TryParse(manifest.Version, out _))
                return $"version \"{manifest.Version}\" isn't valid, expected something like 1.0.0";

            if (manifest.Capabilities.Count == 0)
                return "manifest declares no capabilities";

            if (string.IsNullOrWhiteSpace(manifest.Entry) || Path.IsPathRooted(manifest.Entry) || manifest.Entry.Contains("..", StringComparison.Ordinal))
                return "entry must be a file name inside the mod directory";

            if (!File.Exists(Path.Combine(candidate.Directory, manifest.Entry)))
                return $"entry assembly {manifest.Entry} not found";

            foreach (var dependency in manifest.Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dependency.Id))
                    return "a dependency has no id";

                if (dependency.Id == manifest.Id)
                    return "it depends on itself";

                if (dependency.MinVersion.Length > 0 && !ModVersions.TryParse(dependency.MinVersion, out _))
                    return $"dependency {dependency.Id} has an invalid minVersion \"{dependency.MinVersion}\"";
            }

            return null;
        }

        private static List<ModCandidate> dedupe(List<ModCandidate> candidates, List<SkippedMod> skipped)
        {
            var seen = new HashSet<string>();
            var unique = new List<ModCandidate>();

            foreach (var candidate in candidates)
            {
                if (seen.Add(candidate.Manifest.Id))
                    unique.Add(candidate);
                else
                    skipped.Add(new SkippedMod($"{candidate.Label} in {Path.GetFileName(candidate.Directory)}", "another mod with the same id is installed"));
            }

            return unique;
        }

        private static string? checkRequiredHooks(ModCandidate candidate)
        {
            var missing = candidate.Manifest.Hooks
                                   .Where(h => h.Value == HookRequirement.Required && !FeatureRegistry.IsAvailable(h.Key))
                                   .Select(h => FeatureRegistry.Info(FeatureRegistry.ForHook(h.Key)).Name)
                                   .ToList();

            return missing.Count == 0 ? null : $"needs {string.Join(", ", missing)}, which this osu! version doesn't support";
        }

        private static List<ModCandidate> resolveDependencies(List<ModCandidate> candidates, List<SkippedMod> skipped)
        {
            var remaining = candidates.ToDictionary(c => c.Manifest.Id);
            bool changed = true;

            while (changed)
            {
                changed = false;

                foreach (var candidate in remaining.Values.ToList())
                {
                    foreach (var dependency in candidate.Manifest.Dependencies.Where(d => !d.Optional))
                    {
                        string? problem = !remaining.TryGetValue(dependency.Id, out var target)
                            ? $"needs {dependency.Id}, which isn't installed or can't load"
                            : VersionProblem(dependency, target.Manifest);

                        if (problem == null)
                            continue;

                        remaining.Remove(candidate.Manifest.Id);
                        skipped.Add(new SkippedMod(candidate.Label, problem));
                        changed = true;
                        break;
                    }
                }
            }

            return [.. candidates.Where(c => remaining.ContainsKey(c.Manifest.Id))];
        }

        /// <summary>
        ///     deps before dependents otherwise folder order
        /// </summary>
        private static List<ModCandidate> order(List<ModCandidate> candidates, List<SkippedMod> skipped)
        {
            var ids = candidates.Select(c => c.Manifest.Id).ToHashSet();

            var dependsOn = candidates.ToDictionary(
                c => c.Manifest.Id,
                c => c.Manifest.Dependencies.Select(d => d.Id).Where(ids.Contains).ToHashSet());

            var ordered = new List<ModCandidate>();
            var placed = new HashSet<string>();

            // kahn's algorithm, picking the first ready mod in folder order each round keeps it deterministic
            while (true)
            {
                var next = candidates.FirstOrDefault(c => !placed.Contains(c.Manifest.Id) && dependsOn[c.Manifest.Id].All(placed.Contains));

                if (next == null)
                    break;

                ordered.Add(next);
                placed.Add(next.Manifest.Id);
            }

            foreach (var stuck in candidates.Where(c => !placed.Contains(c.Manifest.Id)))
                skipped.Add(new SkippedMod(stuck.Label, $"dependency cycle with {string.Join(", ", dependsOn[stuck.Manifest.Id].Where(d => !placed.Contains(d)))}"));

            return ordered;
        }

        private static string? checkDependenciesLoaded(ModCandidate candidate)
        {
            var failed = candidate.Manifest.Dependencies.Where(d => !d.Optional && !IsLoaded(d.Id)).Select(d => d.Id).ToList();
            return failed.Count == 0 ? null : $"needs {string.Join(", ", failed)}, which failed to load";
        }

        private static string? load(ModCandidate candidate)
        {
            var manifest = candidate.Manifest;
            var assembly = Assembly.LoadFrom(Path.Combine(candidate.Directory, manifest.Entry));

            var entryTypes = assembly.GetTypes().Where(t => typeof(IMod).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false }).ToList();

            if (entryTypes.Count != 1)
                return $"expected exactly one {nameof(IMod)} implementation, found {entryTypes.Count}";

            foreach (var dependency in manifest.Dependencies.Where(d => d.Optional))
            {
                var target = Get(dependency.Id);

                if (target == null)
                    LoaderLog.Info($"[{manifest.Id}] optional dependency {dependency.Id} isn't loaded, loading without it.");
                else if (VersionProblem(dependency, target.Manifest) is string tooOld)
                    LoaderLog.Info($"[{manifest.Id}] optional dependency {tooOld}, loading without it.");
            }

            var instance = (IMod)Activator.CreateInstance(entryTypes[0])!;
            instance.Load(new ModContext(manifest, candidate.Directory));

            LOADED.Add(new LoadedMod(manifest, instance));
            string author = manifest.Author.Length > 0 ? $" by {manifest.Author}" : string.Empty;
            LoaderLog.Info($"Loaded {manifest.Name} {manifest.Version}{author} [{string.Join(", ", manifest.Capabilities)}].");
            return null;
        }

        /// <summary>null if <paramref name="target"/> satisfies the dependency's minVersion</summary>
        public static string? VersionProblem(ModDependency dependency, ModManifest target)
        {
            if (dependency.MinVersion.Length == 0)
                return null;

            if (!ModVersions.TryParse(dependency.MinVersion, out var min))
                return $"{dependency.Id} has an invalid minVersion";

            if (!ModVersions.TryParse(target.Version, out var actual))
                return $"needs {dependency.Id} {dependency.MinVersion} or newer, installed version \"{target.Version}\" can't be read";

            return actual.ComparePrecedenceTo(min) >= 0 ? null : $"needs {dependency.Id} {dependency.MinVersion} or newer, {target.Version} is installed";
        }

        private static void report(List<SkippedMod> skipped)
        {
            LoaderLog.Info($"Loaded {LOADED.Count} mod(s), skipped {skipped.Count}.");

            foreach (var mod in skipped)
                LoaderLog.Info($"  skipped {mod.Name}: {mod.Reason}");

            if (skipped.Count == 0)
                return;

            string names = string.Join(", ", skipped.Take(5).Select(s => s.Name));
            string more = skipped.Count > 5 ? $" and {skipped.Count - 5} more" : string.Empty;
            LoaderLog.Notify($"{skipped.Count} mod(s) couldn't load: {names}{more}. Check the log for why.");
        }
    }

    internal sealed record ModCandidate(string Directory, ModManifest Manifest)
    {
        public string Label => string.IsNullOrWhiteSpace(Manifest.Name) ? Path.GetFileName(Directory) : Manifest.Name;
    }

    internal sealed record SkippedMod(string Name, string Reason);

    internal sealed record LoadedMod(ModManifest Manifest, IMod Instance);

    internal sealed class ModContext : IModContext
    {
        public ModManifest Manifest { get; }
        public string ModDirectory { get; }
        public IModLogger Log { get; }
        public IUiHooks Ui { get; }

        public ModContext(ModManifest manifest, string modDirectory)
        {
            Manifest = manifest;
            ModDirectory = modDirectory;
            Log = new ModLogger(manifest.Id);
            Ui = new UiHooks(manifest, Log);
        }

        public bool IsHookAvailable(Hook hook) => FeatureRegistry.IsAvailable(hook);

        public bool IsModLoaded(string id)
        {
            var target = ModLoader.Get(id);
            if (target == null)
                return false;

            var dependency = Manifest.Dependencies.FirstOrDefault(d => d.Id == id);
            return dependency == null || ModLoader.VersionProblem(dependency, target.Manifest) == null;
        }
    }
}
