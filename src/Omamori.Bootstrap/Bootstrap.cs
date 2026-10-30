using System;
using System.IO;
using System.Reflection;
using osu.Framework.Logging;

namespace Omamori.Bootstrap
{
    /// <summary>
    ///     lazer blames crashes on ruleset assemblies
    /// </summary>
    internal static class Bootstrap
    {
        /// <summary>bump when the core entry signature changes</summary>
        public const int CONTRACT_VERSION = 1;

        public const string CORE_ASSEMBLY = "Omamori.Core";
        public const string CORE_ENTRY_TYPE = "Omamori.Core.CoreEntry";
        public const string CORE_ENTRY_METHOD = "Start";

        private static readonly object start_lock = new object();
        private static bool started;

        /// <summary>
        ///     called from host ruleset ctor
        /// </summary>
        public static void EnsureStarted(Assembly bootstrapAssembly)
        {
            lock (start_lock)
            {
                if (started)
                    return;

                started = true;
            }

            try
            {
                string dataDirectory = Path.GetDirectoryName(Path.GetDirectoryName(bootstrapAssembly.Location))!;
                string corePath = Path.Combine(dataDirectory, "omamori", "core", $"{CORE_ASSEMBLY}.dll");

                if (!File.Exists(corePath))
                {
                    notify($"Omamori core not found at {corePath}. Running without mods.");
                    return;
                }

                var entry = Assembly.LoadFrom(corePath)
                                    .GetType(CORE_ENTRY_TYPE, throwOnError: true)!
                                    .GetMethod(CORE_ENTRY_METHOD, BindingFlags.Public | BindingFlags.Static);

                if (entry == null)
                {
                    notify($"Omamori core has no {CORE_ENTRY_TYPE}.{CORE_ENTRY_METHOD}. Reinstall Omamori.");
                    return;
                }

                entry.Invoke(null, [CONTRACT_VERSION, dataDirectory, OmamoriRuleset.SHORT_NAME]);
            }
            catch (Exception e)
            {
                var inner = (e as TargetInvocationException)?.InnerException ?? e;
                notify($"Omamori failed to start, running without mods: {inner.Message}");
                Logger.Log($"[Omamori] {inner}");
            }
        }

        // "important" shows up as ingame notification
        private static void notify(string message) => Logger.Log($"[Omamori] {message}", level: LogLevel.Important);
    }
}
