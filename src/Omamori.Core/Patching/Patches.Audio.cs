using HarmonyLib;
using Omamori.Core.Audio;
using Omamori.Core.Features;
using osu.Game;

namespace Omamori.Core.Patching
{
    internal static partial class Patches
    {
        /// <summary>
        ///     the music controller and audio manager live on the game, so mods' audio waits until it has loaded
        /// </summary>
        [Feature(Feature.Audio)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class AudioReady
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance) => FeatureRegistry.Run(Feature.Audio, () => ModAudio.Attach(__instance));
        }
    }
}
