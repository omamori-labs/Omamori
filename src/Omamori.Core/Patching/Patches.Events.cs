using HarmonyLib;
using Omamori.Core.Config;
using Omamori.Core.Events.Sources;
using Omamori.Core.Features;
using osu.Framework.Platform;
using osu.Game;

namespace Omamori.Core.Patching
{
    // read-only, each group subscribes to the game's public state once it has loaded, nothing else is patched
    internal static partial class Patches
    {
        [Feature(Feature.ScreenEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class ScreenEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.ScreenEvents, () => ScreenEventSource.Watch(__instance));
            }
        }

        [Feature(Feature.BeatmapEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class BeatmapEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.BeatmapEvents, () => BeatmapEventSource.Watch(__instance));
            }
        }

        [Feature(Feature.PlayEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class PlayEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.PlayEvents, () => PlayEventSource.Watch(__instance));
            }
        }

        [Feature(Feature.LifecycleEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class LifecycleEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.LifecycleEvents, () => LifecycleEventSource.Watch(__instance));
            }
        }

        /// <summary>
        ///     every real exit goes through here, osu!'s exit button skips OnExiting after its outro, threads are still running
        /// </summary>
        [Feature(Feature.LifecycleEvents)]
        [HarmonyPatch(typeof(GameHost), nameof(GameHost.PerformExit))]
        internal static class GameExitingHook
        {
            [HarmonyPrefix]
            private static void prefix()
            {
                // the debounced save might not get to run otherwise
                ModConfig.FlushAll();
                FeatureRegistry.Run(Feature.LifecycleEvents, LifecycleEventSource.Exiting);
            }
        }

        [Feature(Feature.MusicEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class MusicEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.MusicEvents, () => MusicEventSource.Watch(__instance));
            }
        }

        [Feature(Feature.UserEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class UserEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.UserEvents, () => UserEventSource.Watch(__instance));
            }
        }

        [Feature(Feature.SkinEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class SkinEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.SkinEvents, () => SkinEventSource.Watch(__instance));
            }
        }

        [Feature(Feature.LibraryEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class LibraryEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.LibraryEvents, () => LibraryEventSource.Watch(__instance));
            }
        }

        [Feature(Feature.OverlayEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class OverlayEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.OverlayEvents, () => OverlayEventSource.Watch(__instance));
            }
        }

        [Feature(Feature.MultiplayerEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class MultiplayerEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.MultiplayerEvents, () => MultiplayerEventSource.Watch(__instance));
            }
        }
    }
}
