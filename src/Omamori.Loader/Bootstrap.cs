using System;
using System.IO;
using HarmonyLib;

namespace Omamori.Loader
{
    internal static class Bootstrap
    {
        public const string HARMONY_ID = "omamori.loader";

        private static readonly object start_lock = new object();
        private static bool started;

        /// <summary>
        ///     called from host ruleset ctor, only the first call runs
        /// </summary>
        public static void EnsureStarted(string loaderAssemblyPath)
        {
            lock (start_lock)
            {
                if (started)
                    return;

                started = true;
            }

            try
            {
                // loader is at <data>/rulesets/osu.Game.Rulesets.Omamori.dll
                string rulesetsDirectory = Path.GetDirectoryName(loaderAssemblyPath)!;
                string dataDirectory = Path.GetDirectoryName(rulesetsDirectory)!;
                string modsDirectory = Path.Combine(dataDirectory, "omamori", "mods");

                LoaderLog.Info($"Omamori {typeof(Bootstrap).Assembly.GetName().Version} starting (API {API.ApiInfo.VERSION}).");

                new Harmony(HARMONY_ID).PatchAll(typeof(Bootstrap).Assembly);
                LoaderLog.Info("Game patches applied.");

                ModLoader.LoadAll(modsDirectory);
            }
            catch (Exception e)
            {
                LoaderLog.Error("Omamori failed to start. The game will continue without mods.", e);
            }
        }
    }
}
