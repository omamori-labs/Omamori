using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Omamori.API;
using osu.Game;
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
                FeatureRegistry.Run(Feature.MainMenuUi, () => UiHooks.Populate(Hook.MainMenu, __instance.AddInternal));
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
                FeatureRegistry.Run(Feature.ModLoading, () => ModLoader.LoadAll(CoreEntry.ModsDirectory));
            }
        }

        [Feature(Feature.GlobalOverlay)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class GlobalOverlayHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.GlobalOverlay, () => UiHooks.Populate(Hook.GlobalOverlay, __instance.topMostOverlayContent.Add));
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
    }
}
