using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Omamori.API;
using QuikGraph;
using QuikGraph.Algorithms;

namespace Omamori.Core
{
    /// <summary>
    ///     discover (schema) -> validate -> dedupe -> required hooks -> dependencies + load order -> load
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

            foreach (var candidate in resolveDependencies(candidates, skipped))
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
                    skipped.Add(new SkippedMod(candidate.Label, problem));
            }

            return passed;
        }

        private static string? validate(ModCandidate candidate)
        {
            var manifest = candidate.Manifest;

            if (!ApiVersions.IsCompatible(manifest.ApiVersion, out string? apiProblem))
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

        /// <summary>
        ///     returns the mods that can load, deps before dependents, skips the rest with a reason
        /// </summary>
        private static List<ModCandidate> resolveDependencies(List<ModCandidate> candidates, List<SkippedMod> skipped)
        {
            var byId = candidates.ToDictionary(c => c.Manifest.Id);

            var required = graph(candidates, d => !d.Optional);
            var inCycle = new HashSet<string>();

            foreach (var cycle in cycles(required))
            {
                foreach (string id in cycle)
                {
                    inCycle.Add(id);
                    skipped.Add(new SkippedMod(byId[id].Label, $"dependency cycle with {string.Join(", ", cycle.Where(other => other != id))}"));
                }
            }

            var remaining = candidates.Where(c => !inCycle.Contains(c.Manifest.Id)).ToList();
            var ordering = graph(remaining, _ => true);

            foreach (var cycle in cycles(ordering))
            {
                foreach (var edge in ordering.Edges.Where(e => cycle.Contains(e.Source) && cycle.Contains(e.Target) && isOptional(byId[e.Target], e.Source)).ToList())
                    ordering.RemoveEdge(edge);

                LoaderLog.Info($"Optional dependencies between {string.Join(", ", cycle)} form a loop, ignoring them for load order.");
            }

            var accepted = new List<ModCandidate>();
            var acceptedIds = new HashSet<string>();

            foreach (string id in ordering.SourceFirstTopologicalSort())
            {
                var candidate = byId[id];
                string? problem = null;

                foreach (var dependency in candidate.Manifest.Dependencies.Where(d => !d.Optional))
                {
                    if (!byId.TryGetValue(dependency.Id, out var target))
                        problem = $"needs {dependency.Id}, which isn't installed or can't load";
                    else if (!acceptedIds.Contains(dependency.Id))
                        problem = $"needs {dependency.Id}, which was skipped";
                    else
                        problem = VersionProblem(dependency, target.Manifest);

                    if (problem != null)
                        break;
                }

                if (problem != null)
                {
                    skipped.Add(new SkippedMod(candidate.Label, problem));
                    continue;
                }

                accepted.Add(candidate);
                acceptedIds.Add(id);
            }

            return accepted;
        }

        private static AdjacencyGraph<string, SEdge<string>> graph(List<ModCandidate> candidates, Func<ModDependency, bool> include)
        {
            var result = new AdjacencyGraph<string, SEdge<string>>();
            result.AddVertexRange(candidates.Select(c => c.Manifest.Id));

            foreach (var candidate in candidates)
            {
                foreach (var dependency in candidate.Manifest.Dependencies.Where(include))
                {
                    if (result.ContainsVertex(dependency.Id))
                        result.AddEdge(new SEdge<string>(dependency.Id, candidate.Manifest.Id));
                }
            }

            return result;
        }

        private static List<List<string>> cycles(AdjacencyGraph<string, SEdge<string>> graph)
        {
            var components = new Dictionary<string, int>();
            graph.StronglyConnectedComponents(components);

            return [.. components.GroupBy(c => c.Value).Where(g => g.Count() > 1).Select(g => g.Select(c => c.Key).ToList())];
        }

        private static bool isOptional(ModCandidate dependent, string dependencyId) =>
            dependent.Manifest.Dependencies.Any(d => d.Id == dependencyId && d.Optional);

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
