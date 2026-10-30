using System;
using System.Reflection;
using HarmonyLib;
using McMaster.NETCore.Plugins;
using Omamori.API;
using osu.Framework.Graphics;
using osu.Game;

namespace Omamori.Core
{
    /// <summary>
    ///     each mod gets its own load context so mods can bundle different versions of the same library
    /// </summary>
    internal static class ModIsolation
    {
        /// <summary>
        ///     one type per assembly that must be the same for the game, core and every mod
        /// </summary>
        public static readonly Type[] SHARED_TYPES =
        [
            typeof(IMod), // Omamori.API
            typeof(OsuGame), // osu.Game
            typeof(Drawable), // osu.Framework
            typeof(osuTK.Vector2), // osuTK
            typeof(Harmony), // one harmony for every patch to avoid conflicts
        ];

        public static PluginLoader CreateLoader(string entryPath) =>
            PluginLoader.CreateFromAssemblyFile(
                entryPath,
                isUnloadable: false,
                sharedTypes: SHARED_TYPES,
                configure: config => config.PreferSharedTypes = false);

        /// <summary>for logs, which context an assembly ended up in</summary>
        public static string Describe(Assembly assembly) =>
            $"{assembly.GetName().Name} {assembly.GetName().Version} in {System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly)?.Name ?? "?"}";
    }
}
