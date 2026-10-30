using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using osu.Game;
using osu.Game.Rulesets;
using osu.Game.Screens.Menu;

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
            // ref param can't be captured by a lambda, so this guards itself
            [HarmonyPostfix]
            private static void postfix(ref IEnumerable<RulesetInfo> __result)
            {
                if (!FeatureRegistry.IsActive(Feature.HideHostRuleset))
                    return;

                try
                {
                    FaultInjection.ThrowIfInjected(FaultInjection.RUNTIME, FeatureRegistry.Info(Feature.HideHostRuleset));
                    __result = [.. __result.Where(r => r.ShortName != CoreEntry.HostRulesetShortName)];
                }
                catch (Exception e)
                {
                    FeatureRegistry.Fault(Feature.HideHostRuleset, e);
                }
            }
        }

        [Feature(Feature.MainMenuUi)]
        [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.LoadComplete))]
        internal static class MainMenuInjection
        {
            [HarmonyPostfix]
            private static void postfix(MainMenu __instance)
            {
                FeatureRegistry.Run(Feature.MainMenuUi, () => UiHooks.Populate(UiHooks.MAIN_MENU, __instance.AddInternal));
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
                FeatureRegistry.Run(Feature.GlobalOverlay, () => UiHooks.Populate(UiHooks.GLOBAL_OVERLAY, __instance.topMostOverlayContent.Add));
            }
        }
    }
}
