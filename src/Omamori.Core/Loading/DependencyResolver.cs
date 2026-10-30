using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using QuikGraph;
using QuikGraph.Algorithms;

namespace Omamori.Core
{
    /// <summary>
    ///     which mods can load and in what order, from their required and optional dependencies
    /// </summary>
    internal static class DependencyResolver
    {
        /// <summary>
        ///     returns the mods that can load, deps before dependents, skips the rest with a reason
        /// </summary>
        public static List<ModCandidate> Resolve(List<ModCandidate> candidates, List<SkippedMod> skipped, IReadOnlySet<string>? turnedOff = null)
        {
            var byId = candidates.ToDictionary(c => c.Manifest.Id);

            var required = graph(candidates, d => !d.Optional);
            var inCycle = new HashSet<string>();

            foreach (var cycle in cycles(required))
            {
                foreach (string id in cycle)
                {
                    inCycle.Add(id);
                    skipped.Add(new SkippedMod(byId[id].Label, $"dependency cycle with {string.Join(", ", cycle.Where(other => other != id))}", byId[id].Manifest));
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
                    if (turnedOff?.Contains(dependency.Id) == true)
                        problem = $"needs {dependency.Id}, which is turned off";
                    else if (!byId.TryGetValue(dependency.Id, out var target))
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
                    skipped.Add(new SkippedMod(candidate.Label, problem, candidate.Manifest));
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
    }
}
