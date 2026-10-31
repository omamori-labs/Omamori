using System;
using System.IO;
using System.Reflection;

namespace Omamori.Bootstrap
{
    /// <summary>
    ///     lazer blames crashes on ruleset assemblies
    /// </summary>
    internal static class Bootstrap
    {
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

            Start(Path.GetDirectoryName(Path.GetDirectoryName(bootstrapAssembly.Location)));
        }

        internal static void Start(string? dataDirectory)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dataDirectory);

                string corePath = Layout.CoreAssembly(dataDirectory);

                if (!File.Exists(corePath))
                {
                    LoaderLog.Notify($"Omamori core not found at {corePath}. Running without mods.");
                    return;
                }

                var entry = Assembly.LoadFrom(corePath)
                                    .GetType(Layout.CORE_ENTRY_TYPE, throwOnError: true)!
                                    .GetMethod(Layout.CORE_ENTRY_METHOD, BindingFlags.Public | BindingFlags.Static);

                if (entry == null)
                {
                    LoaderLog.Notify($"Omamori core has no {Layout.CORE_ENTRY_TYPE}.{Layout.CORE_ENTRY_METHOD}. Reinstall Omamori.");
                    return;
                }

                entry.Invoke(null, [Layout.CONTRACT_VERSION, dataDirectory, OmamoriRuleset.SHORT_NAME]);
            }
            catch (Exception e)
            {
                var inner = (e as TargetInvocationException)?.InnerException ?? e;
                LoaderLog.Notify($"Omamori failed to start, running without mods: {inner.Message}");
                LoaderLog.Info(inner.ToString());
            }
        }
    }
}
