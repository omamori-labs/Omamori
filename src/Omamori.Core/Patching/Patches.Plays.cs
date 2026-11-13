using System;
using HarmonyLib;
using Omamori.Core.Features;
using Omamori.Core.Plays;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Screens.Play;
using osu.Game.Screens.Select;

namespace Omamori.Core.Patching
{
    internal static partial class Patches
    {
        /// <summary>
        ///     the one place a solo play starts, rooms only pick a map and the player screen comes later from the room
        /// </summary>
        [Feature(Feature.PlayStarting)]
        [HarmonyPatch(typeof(SoloSongSelect), nameof(SoloSongSelect.OnStart))]
        internal static class PlayStartingInterception
        {
            [HarmonyPrefix]
            private static bool prefix() => FeatureRegistry.Run(Feature.PlayStarting, true, _ => GamePlays.Begin());

            [HarmonyPostfix]
            private static void postfix() => FeatureRegistry.Run(Feature.PlayStarting, GamePlays.End);
        }

        /// <summary>
        ///     the loader makes a new player for every retry, so the decision from song select is carried over to each of them
        /// </summary>
        [Feature(Feature.PlayStarting)]
        [HarmonyPatch(typeof(PlayerLoader), MethodType.Constructor, [typeof(Func<Player>)])]
        internal static class PlayStartingTagging
        {
            [HarmonyPrefix]
            private static void prefix(ref Func<Player> __0)
            {
                __0 = FeatureRegistry.Run(Feature.PlayStarting, __0, create =>
                {
                    if (GamePlays.Current is not { SuppressionReason: not null } starting)
                        return create;

                    return () =>
                    {
                        var player = create();
                        GamePlays.Tag(player, starting);
                        return player;
                    };
                });
            }
        }

        /// <summary>
        ///     no token means lazer never submits, the same path it takes for maps that aren't on the website
        /// </summary>
        [Feature(Feature.PlayStarting)]
        [HarmonyPatch(typeof(SoloPlayer), nameof(SoloPlayer.CreateTokenRequest))]
        internal static class SubmissionSuppression
        {
            [HarmonyPrefix]
            private static bool prefix(SoloPlayer __instance, ref APIRequest<APIScoreToken>? __result)
            {
                bool suppressed = FeatureRegistry.Run(Feature.PlayStarting, false, _ =>
                {
                    if (!GamePlays.IsSuppressed(__instance, out string? reason))
                        return false;

                    LoaderLog.Info($"Not submitting this play: {reason}.");
                    return true;
                });

                if (suppressed)
                    __result = null;

                return !suppressed;
            }
        }
    }
}
