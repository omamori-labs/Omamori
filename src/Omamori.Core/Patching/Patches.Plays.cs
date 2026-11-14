using System;
using HarmonyLib;
using Omamori.Core.Features;
using Omamori.Core.Plays;
using osu.Game.Beatmaps;
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

            // a finalizer runs even when OnStart throws, so the decision never leaks into a later play
            [HarmonyFinalizer]
            private static void finalizer() => GamePlays.End();
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
        ///     every player makes its own playable beatmap, so a late start cuts it down here before anything is drawn
        /// </summary>
        [Feature(Feature.PlayStarting)]
        [HarmonyPatch(typeof(Player), nameof(Player.loadPlayableBeatmap))]
        internal static class LateStartBeatmap
        {
            [HarmonyPostfix]
            private static void postfix(Player __instance, IBeatmap? __result)
            {
                FeatureRegistry.Run(Feature.PlayStarting, () =>
                {
                    if (__result != null)
                        GamePlays.PrepareStart(__instance, __result);
                });
            }
        }

        /// <summary>
        ///     lazer starts every play from the beginning, a late one jumps ahead and waits for the player to resume
        /// </summary>
        [Feature(Feature.PlayStarting)]
        [HarmonyPatch(typeof(SubmittingPlayer), nameof(SubmittingPlayer.StartGameplay))]
        internal static class LateStartGameplay
        {
            [HarmonyPrefix]
            private static void prefix(SubmittingPlayer __instance)
            {
                FeatureRegistry.Run(Feature.PlayStarting, () =>
                {
                    if (GamePlays.StartTimeFor(__instance) is { } start)
                        __instance.SetGameplayStartTime(start);
                });
            }

            [HarmonyPostfix]
            private static void postfix(SubmittingPlayer __instance)
            {
                FeatureRegistry.Run(Feature.PlayStarting, () =>
                {
                    if (GamePlays.StartTimeFor(__instance) != null)
                        LateStart.HoldForResume(__instance);
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
            // false once another prefix has already kept the play from being submitted
            [HarmonyPrefix]
            private static bool prefix(SoloPlayer __instance, ref APIRequest<APIScoreToken>? __result, bool __runOriginal) =>
                __runOriginal && submits(GamePlays.Unsubmitted(__instance), ref __result);
        }

        // false skips CreateTokenRequest with a null token, so the play runs but is never sent
        private static bool submits(string? unsubmittedBecause, ref APIRequest<APIScoreToken>? result)
        {
            if (unsubmittedBecause == null)
                return true;

            LoaderLog.Info($"Not submitting this play: {unsubmittedBecause}.");
            result = null;
            return false;
        }
    }
}
