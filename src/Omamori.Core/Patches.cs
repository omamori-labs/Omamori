using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Omamori.API;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings.Sections;
using osu.Game.Rulesets;
using osu.Game.Screens.Menu;
using osu.Game.Utils;

namespace Omamori.Core
{
    /// <summary>
    ///     patch methods are marked [HarmonyPrefix]/[HarmonyPostfix] instead of relying on harmonys name convention, so they can be camelCase like every private method
    ///     every patch class needs a <see cref="FeatureAttribute"/>, and its code goes through <see cref="FeatureRegistry"/> so a throw turns the feature off
    /// </summary>
    internal static class Patches
    {
        /// <summary>
        ///     hides host ruleset from toolbar, settings, etc
        /// </summary>
        [Feature(Feature.HideHostRuleset)]
        [HarmonyPatch(typeof(RealmRulesetStore), nameof(RealmRulesetStore.AvailableRulesets), MethodType.Getter)]
        internal static class HideHostRuleset
        {
            [HarmonyPostfix]
            private static void postfix(ref IEnumerable<RulesetInfo> __result)
            {
                __result = FeatureRegistry.Run(Feature.HideHostRuleset, __result, rulesets => [.. rulesets.Where(r => r.ShortName != CoreEntry.HostRulesetShortName)]);
            }
        }

        [Feature(Feature.MainMenuUi)]
        [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.LoadComplete))]
        internal static class MainMenuInjection
        {
            [HarmonyPostfix]
            private static void postfix(MainMenu __instance)
            {
                FeatureRegistry.Run(Feature.MainMenuUi, () => UiHooks.Populate(Hook.MainMenu, __instance));
            }
        }

        /// <summary>
        ///     before the game's LoadComplete, mods are out of lazer's startup realm write and registered
        /// </summary>
        [Feature(Feature.ModLoading)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class ModLoadingHook
        {
            [HarmonyPrefix]
            private static void prefix()
            {
                FeatureRegistry.Run(Feature.ModLoading, () => ModLoader.LoadAll(CoreEntry.ModsDirectory, FaultHistory.Open(CoreEntry.FaultsFile), ModSettings.Open(CoreEntry.SettingsFile)));
            }
        }

        [Feature(Feature.GlobalOverlay)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class GlobalOverlayHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.GlobalOverlay, () => UiHooks.Populate(Hook.GlobalOverlay, __instance.topMostOverlayContent));
            }
        }

        /// <summary>
        ///     read-only, the game's public screen stack and bindables, nothing is patched beyond this hook
        /// </summary>
        [Feature(Feature.ScreenEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class ScreenEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.ScreenEvents, () => GameEventSources.WatchScreens(__instance));
            }
        }

        [Feature(Feature.BeatmapEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class BeatmapEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.BeatmapEvents, () => GameEventSources.WatchBeatmap(__instance));
            }
        }

        [Feature(Feature.PlayEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class PlayEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.PlayEvents, () => GameEventSources.WatchPlays(__instance));
            }
        }

        [Feature(Feature.LifecycleEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class LifecycleEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.LifecycleEvents, () => GameEventSources.WatchWindow(__instance));
            }
        }

        [Feature(Feature.MusicEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class MusicEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.MusicEvents, () => GameEventSources.WatchMusic(__instance));
            }
        }

        [Feature(Feature.UserEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class UserEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.UserEvents, () => GameEventSources.WatchUser(__instance));
            }
        }

        [Feature(Feature.SkinEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class SkinEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.SkinEvents, () => GameEventSources.WatchSkin(__instance));
            }
        }

        [Feature(Feature.LibraryEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class LibraryEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.LibraryEvents, () => GameEventSources.WatchLibrary(__instance));
            }
        }

        [Feature(Feature.OverlayEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class OverlayEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.OverlayEvents, () => GameEventSources.WatchOverlays(__instance));
            }
        }

        [Feature(Feature.MultiplayerEvents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class MultiplayerEventsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.MultiplayerEvents, () => GameEventSources.WatchMultiplayer(__instance));
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
                FeatureRegistry.Run(Feature.LifecycleEvents, GameEventSources.Exiting);
            }
        }

        /// <summary>
        ///     sheared overlays sit beside first-run setup, they need the screen footer
        /// </summary>
        [Feature(Feature.ModManager)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class ModManagerHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.ModManager, () =>
                {
                    var overlay = new ModManagerOverlay();
                    __instance.footerBasedOverlayContent.Add(overlay);
                    ModManagerOverlay.Current = overlay;
                });
            }
        }

        /// <summary>
        ///     sections only load when settings first opens, so inserting into the pending list is enough
        /// </summary>
        [Feature(Feature.SettingsModList)]
        [HarmonyPatch(typeof(SettingsOverlay), nameof(SettingsOverlay.load))]
        internal static class SettingsModList
        {
            [HarmonyPostfix]
            private static void postfix(SettingsOverlay __instance)
            {
                FeatureRegistry.Run(Feature.SettingsModList, () =>
                {
                    var sections = __instance.loadableSections;
                    int maintenance = sections.FindIndex(s => s is MaintenanceSection);

                    sections.Insert(maintenance < 0 ? sections.Count : maintenance, new ModListSection());
                });
            }
        }

        /// <summary>
        ///     crashes involving omamori or a mod aren't sent to ppy
        /// </summary>
        [Feature(Feature.CrashReportFilter)]
        [HarmonyPatch(typeof(SentryLogger), nameof(SentryLogger.shouldSubmitException))]
        internal static class CrashReportFilter
        {
            [HarmonyPostfix]
            private static void postfix(Exception exception, ref bool __result)
            {
                __result = FeatureRegistry.Run(Feature.CrashReportFilter, __result, submit =>
                {
                    if (!submit || CrashAttribution.Involvement(exception) is not string involved)
                        return submit;

                    LoaderLog.Info($"Not reporting {exception.GetType().Name} to ppy, it involves {involved}.");
                    return false;
                });
            }
        }

        /// <summary>
        ///     an exception from a mod turns that mod off instead of counting towards osu!'s crash limit
        /// </summary>
        [Feature(Feature.ModContainment)]
        [HarmonyPatch(typeof(OsuGameBase), nameof(OsuGameBase.onExceptionThrown))]
        internal static class ModContainment
        {
            [HarmonyPrefix]
            private static bool prefix(OsuGameBase __instance, Exception ex, ref bool __result)
            {
                bool contained = FeatureRegistry.Run(Feature.ModContainment, false, _ => ModFaults.TryContain(ex, __instance.Host.UpdateThread.Scheduler));

                if (contained)
                    __result = true;

                return !contained;
            }
        }
    }
}
