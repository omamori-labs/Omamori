using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game;
using osu.Game.Rulesets;
using osu.Game.Screens.Menu;

namespace Omamori.Loader
{
    internal static class Patches
    {
        private static readonly MethodInfo add_internal = AccessTools.Method(typeof(CompositeDrawable), "AddInternal");

        private static void addInternal(CompositeDrawable target, Drawable drawable) => add_internal.Invoke(target, new object[] { drawable });

        /// <summary>
        ///     hides host ruleset from toolbar, settings, etc
        /// </summary>
        [HarmonyPatch(typeof(RealmRulesetStore), nameof(RealmRulesetStore.AvailableRulesets), MethodType.Getter)]
        internal static class HideHostRuleset
        {
            private static void Postfix(ref IEnumerable<RulesetInfo> __result)
            {
                __result = __result.Where(r => r.ShortName != OmamoriRuleset.SHORT_NAME).ToList();
            }
        }

        [HarmonyPatch(typeof(MainMenu), "LoadComplete")]
        internal static class MainMenuInjection
        {
            private static void Postfix(MainMenu __instance)
            {
                UiHooks.Populate(UiHooks.MAIN_MENU, d => addInternal(__instance, d));
            }
        }

        [HarmonyPatch(typeof(OsuGame), "LoadComplete")]
        internal static class GlobalOverlayInjection
        {
            private static readonly FieldInfo top_most_overlay_content = AccessTools.Field(typeof(OsuGame), "topMostOverlayContent");

            private static void Postfix(OsuGame __instance)
            {
                var container = (Container)top_most_overlay_content.GetValue(__instance)!;
                UiHooks.Populate(UiHooks.GLOBAL_OVERLAY, container.Add);
            }
        }
    }
}
