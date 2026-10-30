using System;
using System.Collections.Generic;
using System.Linq;

namespace Omamori.Core
{
    /// <summary>
    ///     testing only
    /// </summary>
    internal static class FaultInjection
    {
        public const string ENVIRONMENT_VARIABLE = "OMAMORI_FAULTS";

        public const string APPLY = "apply";
        public const string RUNTIME = "runtime";

        private static readonly HashSet<string> faults = parse(Environment.GetEnvironmentVariable(ENVIRONMENT_VARIABLE));

        public static bool Any => faults.Count > 0;

        public static void ThrowIfInjected(string stage, FeatureInfo feature)
        {
            if (faults.Contains($"{stage}:{feature.Id}"))
                throw new InjectedFaultException($"injected {stage} fault for {feature.Id}");
        }

        private static HashSet<string> parse(string? value) =>
            [.. (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(f => f.ToLowerInvariant())];
    }
}
