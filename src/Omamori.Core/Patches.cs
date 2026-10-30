using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using osu.Game;
using osu.Game.Rulesets;
using osu.Game.Screens.Menu;

namespace Omamori.Core
{
    internal static class Patches
    {
        /// <summary>
        ///     hides host ruleset from toolbar, settings, etc
        /// </summary>
        [HarmonyPatch(typeof(RealmRulesetStore), nameof(RealmRulesetStore.AvailableRulesets), MethodType.Getter)]
        internal static class HideHostRuleset
        {
            private static void Postfix(ref IEnumerable<RulesetInfo> __result)
            {
                __result = __result.Where(r => r.ShortName != CoreEntry.HostRulesetShortName).ToList();
            }
        }

        [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.LoadComplete))]
        internal static class MainMenuInjection
        {
            private static void Postfix(MainMenu __instance)
            {
                UiHooks.Populate(UiHooks.MAIN_MENU, __instance.AddInternal);
            }
        }

        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class GameLoadHooks
        {
            private static void Prefix()
            {
                ModLoader.LoadAll(CoreEntry.ModsDirectory);
            }

            private static void Postfix(OsuGame __instance)
            {
                UiHooks.Populate(UiHooks.GLOBAL_OVERLAY, __instance.topMostOverlayContent.Add);
            }
        }
    }
}
