using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using McMaster.NETCore.Plugins;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Framework.Platform;

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

        private static readonly List<LoadedMod> loaded_mods = [];
        private static readonly List<ModCandidate> turned_off_mods = [];
        private static readonly List<SkippedMod> skipped_mods = [];

        // mods load on the update thread, but a crash on any thread looks up which mod it came from
        private static readonly object list_lock = new object();

        /// <summary>a copy, in load order</summary>
        public static IReadOnlyList<LoadedMod> Loaded => snapshot(loaded_mods);

        public static IReadOnlyList<ModCandidate> TurnedOff => snapshot(turned_off_mods);

        public static IReadOnlyList<SkippedMod> Skipped => snapshot(skipped_mods);

        /// <summary>null until mods load</summary>
        public static ModSwitches? Switches { get; internal set; }

        public static bool IsLoaded(string id) => Get(id) != null;

        public static LoadedMod? Get(string id)
        {
            lock (list_lock)
                return loaded_mods.FirstOrDefault(m => m.Manifest.Id == id);
        }

        public static void LoadAll(string modsDirectory, FaultHistory history, ModSwitches switches, GameHost? host = null)
        {
            ModFaults.History = history;
            Switches = switches;

            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                LoaderLog.Info($"Created mods directory at {modsDirectory}.");
                return;
            }

            var skipped = new List<SkippedMod>();

            try
            {
                var candidates = discover(modsDirectory, skipped);
                candidates = filter(candidates, skipped, validate);
                candidates = dedupe(candidates, skipped);

                lock (list_lock)
                    turned_off_mods.AddRange(candidates.Where(c => !switches.IsEnabled(c.Manifest.Id)));

                candidates = [.. candidates.Where(c => switches.IsEnabled(c.Manifest.Id))];

                candidates = filter(candidates, skipped, c => history.SkipReason(c.Manifest));
                candidates = filter(candidates, skipped, checkRequiredHooks);

                foreach (var candidate in DependencyResolver.Resolve(candidates, skipped, TurnedOff.Select(c => c.Manifest.Id).ToHashSet()))
                {
                    try
                    {
                        string? problem = checkDependenciesLoaded(candidate) ?? load(candidate, host);

                        if (problem != null)
                            skipped.Add(new SkippedMod(candidate.Label, problem, candidate.Manifest));
                    }
                    catch (Exception e)
                    {
                        LoaderLog.Info(e.ToString());
                        skipped.Add(new SkippedMod(candidate.Label, $"failed to load: {(e as TargetInvocationException)?.InnerException?.Message ?? e.Message}", candidate.Manifest));
                    }
                }
            }
            finally
            {
                // also when loading throws part way, so the manager still lists what was skipped up to then
                lock (list_lock)
                    skipped_mods.AddRange(skipped);
            }

            report(skipped);
        }

        internal static void Add(LoadedMod mod)
        {
            lock (list_lock)
                loaded_mods.Add(mod);
        }

        internal static void Clear()
        {
            lock (list_lock)
            {
                loaded_mods.Clear();
                turned_off_mods.Clear();
                skipped_mods.Clear();
            }

            Switches = null;
        }

        private static IReadOnlyList<T> snapshot<T>(List<T> list)
        {
            lock (list_lock)
                return [.. list];
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

            if (ManifestRules.ApiProblem(manifest.ApiVersion) is string apiProblem)
                return apiProblem;

            if (!File.Exists(Path.Combine(candidate.Directory, manifest.Entry)))
                return $"entry assembly {manifest.Entry} not found";

            if (ManifestRules.DependsOnItself(manifest.Id, manifest.Dependencies.Select(d => d.Id)))
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

        private static string? load(ModCandidate candidate, GameHost? host)
        {
            var manifest = candidate.Manifest;
            var loader = ModIsolation.CreateLoader(Path.Combine(candidate.Directory, manifest.Entry));

            IEnumerable<LoadedMod> loadedDependencies() => manifest.Dependencies.Select(d => Get(d.Id)).OfType<LoadedMod>();

            ModIsolation.ShareDependencies(loader, loadedDependencies);

            foreach (var dependency in loadedDependencies())
                warnIfBundled(candidate, dependency.Manifest);

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

            IMod instance = null!;

            RunLoad(manifest.Id, loader, () =>
            {
                instance = (IMod)Activator.CreateInstance(entryTypes[0])!;
                instance.Load(new ModContext(manifest, candidate.Directory, CoreEntry.ModDataDirectory(manifest.Id), host));
            });

            // catches mods that bring their own harmony
            if (!ManifestRules.DeclaresPatches(manifest.Capabilities) && PatchRemoval.RemoveFrom(loader.LoadContext) > 0)
            {
                ModFaults.TurnOff(manifest.Id);
                return "it patches osu! but doesn't list Patches in its capabilities";
            }

            Add(new LoadedMod(manifest, instance, loader, candidate.Label));

            string[] facts = [manifest.Name, manifest.Version, ManifestText.Byline(manifest)];
            LoaderLog.Info($"Loaded {string.Join(' ', facts.Where(f => f.Length > 0))} [{string.Join(", ", manifest.Capabilities)}].");
            return null;
        }

        private static void warnIfBundled(ModCandidate candidate, ModManifest dependency)
        {
            if (!File.Exists(Path.Combine(candidate.Directory, Path.GetFileName(dependency.Entry))))
                return;

            LoaderLog.Info($"[{candidate.Manifest.Id}] bundles its own copy of {dependency.Entry}, services from {dependency.Id} won't match. "
                           + "Reference it with OmamoriModReference instead.");
        }

        /// <summary>
        ///     a throw turns the mod off and removes its patches, so nothing it registered before throwing runs, then rethrows
        /// </summary>
        internal static void RunLoad(string id, PluginLoader loader, Action load)
        {
            try
            {
                load();
            }
            catch
            {
                ModFaults.TurnOff(id);
                PatchRemoval.RemoveFrom(loader.LoadContext);
                throw;
            }
        }

        private static void report(List<SkippedMod> skipped)
        {
            LoaderLog.Info($"Loaded {Loaded.Count} mod(s), skipped {skipped.Count}, {TurnedOff.Count} turned off in settings.");

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
