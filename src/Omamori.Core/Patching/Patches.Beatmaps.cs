using HarmonyLib;
using Omamori.Core.Beatmaps;
using Omamori.Core.Features;
using osu.Game;

namespace Omamori.Core.Patching
{
    internal static partial class Patches
    {
        /// <summary>
        ///     the selected beatmap, ruleset and mods live on the game, so reading them waits until it has loaded
        /// </summary>
        [Feature(Feature.Beatmaps)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class BeatmapsReady
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance) => FeatureRegistry.Run(Feature.Beatmaps, () => GameBeatmaps.Attach(__instance));
        }
    }
}
