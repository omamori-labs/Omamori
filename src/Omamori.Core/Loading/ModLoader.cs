using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;

namespace Omamori.Core.Loading
{
    /// <summary>
    ///     discover (schema) -> validate -> dedupe -> turned off in settings -> crash history -> required hooks -> dependencies + load order -> load
    /// </summary>
    internal static class ModLoader
    {
        public const string MANIFEST_FILENAME = "mod.json";

        private static readonly JsonSerializerOptions json_options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
        };

        public static readonly List<LoadedMod> LOADED = [];

        public static readonly List<ModCandidate> TURNED_OFF = [];

        public static readonly List<SkippedMod> SKIPPED = [];

        /// <summary>null until mods load</summary>
        public static ModSwitches? Switches { get; private set; }

        public static bool IsLoaded(string id) => LOADED.Any(m => m.Manifest.Id == id);

        public static LoadedMod? Get(string id) => LOADED.FirstOrDefault(m => m.Manifest.Id == id);

        public static void LoadAll(string modsDirectory, FaultHistory history, ModSwitches switches)
        {
            ModFaults.History = history;
            Switches = switches;

            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                LoaderLog.Info($"Created mods directory at {modsDirectory}.");
                return;
            }

            var candidates = discover(modsDirectory, SKIPPED);
            candidates = filter(candidates, SKIPPED, validate);
            candidates = dedupe(candidates, SKIPPED);

            TURNED_OFF.AddRange(candidates.Where(c => !switches.IsEnabled(c.Manifest.Id)));
            candidates = [.. candidates.Where(c => switches.IsEnabled(c.Manifest.Id))];

            candidates = filter(candidates, SKIPPED, c => history.SkipReason(c.Manifest));
            candidates = filter(candidates, SKIPPED, checkRequiredHooks);

            foreach (var candidate in DependencyResolver.Resolve(candidates, SKIPPED, TURNED_OFF.Select(c => c.Manifest.Id).ToHashSet()))
            {
                try
                {
                    string? problem = checkDependenciesLoaded(candidate) ?? load(candidate);

                    if (problem != null)
                        SKIPPED.Add(new SkippedMod(candidate.Label, problem, candidate.Manifest));
                }
                catch (Exception e)
                {
                    LoaderLog.Info(e.ToString());
                    SKIPPED.Add(new SkippedMod(candidate.Label, $"failed to load: {(e as TargetInvocationException)?.InnerException?.Message ?? e.Message}", candidate.Manifest));
                }
            }

            report(SKIPPED);
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
                    using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
                    var problems = ManifestSchema.Validate(document.RootElement);

                    if (problems.Count > 0)
                    {
                        foreach (string problem in problems)
                            LoaderLog.Info($"  {folder}/{MANIFEST_FILENAME} {problem}");

                        skipped.Add(new SkippedMod(folder, $"{MANIFEST_FILENAME} is invalid: {ManifestSchema.Summarise(problems)}"));
                        continue;
                    }

                    var manifest = document.RootElement.Deserialize<ModManifest>(json_options)
                                   ?? throw new InvalidDataException("manifest is empty");

                    candidates.Add(new ModCandidate(directory, manifest));
                }
                catch (Exception e)
                {
                    skipped.Add(new SkippedMod(folder, $"unreadable {MANIFEST_FILENAME}: {e.Message}"));
                }
            }

            return candidates;
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
                    skipped.Add(new SkippedMod(candidate.Label, problem, candidate.Manifest));
            }

            return passed;
        }

        private static string? validate(ModCandidate candidate)
        {
            var manifest = candidate.Manifest;

            if (!ApiVersions.IsCompatible(manifest.ApiVersion, ApiInfo.VERSION, out string? apiProblem))
                return apiProblem;

            if (!File.Exists(Path.Combine(candidate.Directory, manifest.Entry)))
                return $"entry assembly {manifest.Entry} not found";

            if (manifest.Dependencies.Any(d => d.Id == manifest.Id))
                return "it depends on itself";

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

        private static string? checkDependenciesLoaded(ModCandidate candidate)
        {
            var failed = candidate.Manifest.Dependencies.Where(d => !d.Optional && !IsLoaded(d.Id)).Select(d => d.Id).ToList();
            return failed.Count == 0 ? null : $"needs {string.Join(", ", failed)}, which failed to load";
        }

        private static string? load(ModCandidate candidate)
        {
            var manifest = candidate.Manifest;
            var loader = ModIsolation.CreateLoader(Path.Combine(candidate.Directory, manifest.Entry));
            ModIsolation.ShareDependencies(loader, () => manifest.Dependencies.Select(d => Get(d.Id)).OfType<LoadedMod>());

            foreach (var dependency in manifest.Dependencies.Select(d => Get(d.Id)).OfType<LoadedMod>())
            {
                if (File.Exists(Path.Combine(candidate.Directory, Path.GetFileName(dependency.Manifest.Entry))))
                    LoaderLog.Info($"[{manifest.Id}] bundles its own copy of {dependency.Manifest.Entry}, services from {dependency.Manifest.Id} won't match. Reference it with OmamoriModReference instead.");
            }

            var assembly = loader.LoadDefaultAssembly();

            var entryTypes = assembly.GetTypes().Where(t => typeof(IMod).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false }).ToList();

            if (entryTypes.Count != 1)
                return $"expected exactly one {nameof(IMod)} implementation, found {entryTypes.Count}";

            foreach (var dependency in manifest.Dependencies.Where(d => d.Optional))
            {
                var target = Get(dependency.Id);

                if (target == null)
                    LoaderLog.Info($"[{manifest.Id}] optional dependency {dependency.Id} isn't loaded, loading without it.");
                else if (DependencyResolver.VersionProblem(dependency, target.Manifest) is string tooOld)
                    LoaderLog.Info($"[{manifest.Id}] optional dependency {tooOld}, loading without it.");
            }

            var instance = (IMod)Activator.CreateInstance(entryTypes[0])!;
            instance.Load(new ModContext(manifest, candidate.Directory));

            LOADED.Add(new LoadedMod(manifest, instance, loader));
            string author = manifest.Author.Length > 0 ? $" by {manifest.Author}" : string.Empty;
            LoaderLog.Info($"Loaded {manifest.Name} {manifest.Version}{author} [{string.Join(", ", manifest.Capabilities)}].");
            return null;
        }

        private static void report(List<SkippedMod> skipped)
        {
            LoaderLog.Info($"Loaded {LOADED.Count} mod(s), skipped {skipped.Count}, {TURNED_OFF.Count} turned off in settings.");

            foreach (var mod in skipped)
                LoaderLog.Info($"  skipped {mod.Name}: {mod.Reason}");

            if (skipped.Count == 0)
                return;

            string names = string.Join(", ", skipped.Take(5).Select(s => s.Name));
            string more = skipped.Count > 5 ? $" and {skipped.Count - 5} more" : string.Empty;
            LoaderLog.Notify($"{skipped.Count} mod(s) couldn't load: {names}{more}. Check the log for why.");
        }
    }
}
