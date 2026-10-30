using HarmonyLib;
using Omamori.API;
using osu.Game;
using osu.Game.Screens.Menu;

namespace Omamori.Core
{
    // where mods' own drawables go, see UiHooks
    internal static partial class Patches
    {
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
    }
}
