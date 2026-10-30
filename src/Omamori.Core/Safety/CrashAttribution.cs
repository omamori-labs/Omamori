using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;

namespace Omamori.Core.Safety
{
    /// <summary>
    ///     decides whether a crash is omamori's or a mod's, so it isn't reported to ppy as an osu! bug
    ///     a crash a mod causes without its code being on the stack can't be told apart and still reports, that's the accepted tradeoff
    /// </summary>
    internal static class CrashAttribution
    {
        private static readonly string[] omamori_assemblies = ["osu.Game.Rulesets.Omamori", "Omamori.Core", "Omamori.API"];

        public static string? Involvement(Exception? exception) => Involvement(exception, isOurs);

        internal static string? Involvement(Exception? exception, Func<Assembly, bool> isOurAssembly)
        {
            foreach (var frame in Frames(exception))
            {
                var method = frame.GetMethod();
                var assembly = method?.DeclaringType?.Assembly;

                if (assembly != null && isOurAssembly(assembly))
                    return assembly.GetName().Name;

                if (method != null && Harmony.GetOriginalMethodFromStackframe(frame) is { } original && original != method && Harmony.GetPatchInfo(original) != null)
                    return $"{original.DeclaringType?.Name}.{original.Name}, which is patched";
            }

            return null;
        }

        internal static IEnumerable<StackFrame> Frames(Exception? exception) => flatten(exception).SelectMany(e => new StackTrace(e).GetFrames());

        // omamori itself
        private static bool isOurs(Assembly assembly) =>
            omamori_assemblies.Contains(assembly.GetName().Name)
            || (AssemblyLoadContext.GetLoadContext(assembly) is { } context && context != AssemblyLoadContext.Default);

        private static IEnumerable<Exception> flatten(Exception? exception)
        {
            if (exception == null)
                yield break;

            yield return exception;

            if (exception is AggregateException aggregate)
            {
                foreach (var child in aggregate.InnerExceptions)
                {
                    foreach (var nested in flatten(child))
                        yield return nested;
                }
            }
            else
            {
                foreach (var nested in flatten(exception.InnerException))
                    yield return nested;
            }
        }
    }
}
