using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Omamori.Core.Features;
using Omamori.Core.Loading;
using Omamori.Core.Safety;
using osu.Game;
using osu.Game.Rulesets;

namespace Omamori.Core.Patching
{
    internal static partial class Patches
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
                FeatureRegistry.Run(Feature.ModLoading, () => ModLoader.LoadAll(CoreEntry.ModsDirectory, FaultHistory.Open(CoreEntry.FaultsFile), ModSwitches.Open(CoreEntry.SwitchesFile)));
            }
        }
    }
}
