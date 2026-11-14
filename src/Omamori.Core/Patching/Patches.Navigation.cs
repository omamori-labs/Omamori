using HarmonyLib;
using Omamori.Core.Features;
using Omamori.Core.Navigation;
using osu.Game;
using osu.Game.Screens.Select;

namespace Omamori.Core.Patching
{
    internal static partial class Patches
    {
        /// <summary>
        ///     after mods load, so navigation only works once the game it moves around is ready
        /// </summary>
        [Feature(Feature.Navigation)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class NavigationReady
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance) => FeatureRegistry.Run(Feature.Navigation, () => GameNavigation.Attach(__instance));
        }

        [Feature(Feature.Navigation)]
        [HarmonyPatch(typeof(SongSelect), nameof(SongSelect.OnEntering))]
        internal static class NavigationSongSelectEntered
        {
            [HarmonyPostfix]
            private static void postfix(SongSelect __instance) => FeatureRegistry.Run(Feature.Navigation, () => GameNavigation.SongSelectEntered(__instance));
        }
    }
}
