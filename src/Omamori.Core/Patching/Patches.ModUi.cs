using HarmonyLib;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Input;
using Omamori.Core.ModUi;
using Omamori.Core.Notifications;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Play;
using osu.Game.Screens.Ranking;
using osu.Game.Screens.Select;

namespace Omamori.Core.Patching
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

        // on the base class so solo and multiplayer song select both get it
        [Feature(Feature.SongSelectUi)]
        [HarmonyPatch(typeof(SongSelect), nameof(SongSelect.LoadComplete))]
        internal static class SongSelectInjection
        {
            [HarmonyPostfix]
            private static void postfix(SongSelect __instance)
            {
                FeatureRegistry.Run(Feature.SongSelectUi, () => UiHooks.Populate(Hook.SongSelect, __instance));
            }
        }

        [Feature(Feature.ResultsUi)]
        [HarmonyPatch(typeof(ResultsScreen), nameof(ResultsScreen.LoadComplete))]
        internal static class ResultsInjection
        {
            [HarmonyPostfix]
            private static void postfix(ResultsScreen __instance)
            {
                FeatureRegistry.Run(Feature.ResultsUi, () => UiHooks.Populate(Hook.Results, __instance));
            }
        }

        [Feature(Feature.Notifications)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class NotificationsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.Notifications, () => NotificationSink.Attach(__instance.Notifications));
            }
        }

        [Feature(Feature.KeyBindings)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class KeyBindingsHook
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.KeyBindings, () => __instance.topMostOverlayContent.Add(new KeyListener()));
            }
        }

        [Feature(Feature.GameplayHudUi)]
        [HarmonyPatch(typeof(HUDOverlay), nameof(HUDOverlay.LoadComplete))]
        internal static class GameplayHudInjection
        {
            [HarmonyPostfix]
            private static void postfix(HUDOverlay __instance)
            {
                FeatureRegistry.Run(Feature.GameplayHudUi, () =>
                {
                    var layer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = __instance.ShowHud.Value ? 1 : 0,
                    };

                    __instance.Add(layer);
                    __instance.hideTargets.Add(layer);

                    UiHooks.Populate(Hook.GameplayHud, layer);
                });
            }
        }
    }
}
