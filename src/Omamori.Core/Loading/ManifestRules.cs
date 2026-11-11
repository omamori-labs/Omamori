using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;

namespace Omamori.Core.Loading
{
    /// <summary>
    ///     what the schema can't say, compiled into the loader and the build task so a mod that builds also loads
    /// </summary>
    internal static class ManifestRules
    {
        /// <summary>null when a mod built for <paramref name="apiVersion"/> runs on this Omamori</summary>
        public static string? ApiProblem(string? apiVersion) =>
            ApiVersions.IsCompatible(apiVersion, ApiInfo.VERSION, out string? problem) ? null : problem;

        public static bool DependsOnItself(string id, IEnumerable<string?> dependencyIds) => dependencyIds.Contains(id, StringComparer.Ordinal);

        public static bool DeclaresPatches(IEnumerable<ModCapability> capabilities) => capabilities.Contains(ModCapability.Patches);

        /// <summary>capabilities as mod.json spells them, for the build task which only has the json</summary>
        public static bool DeclaresPatches(IEnumerable<string?> capabilities) => capabilities.Contains(nameof(ModCapability.Patches), StringComparer.Ordinal);
    }
}
