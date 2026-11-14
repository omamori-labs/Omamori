using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Input;
using Omamori.Core.ModUi;
using Omamori.Core.Notifications;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Game;
using osu.Game.Online.Leaderboards;
using osu.Game.Rulesets;
using osu.Game.Overlays.Toolbar;
using osu.Game.Screens.Footer;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Play;
using osu.Game.Screens.Ranking;
using osu.Game.Screens.Select;
using osu.Game.Skinning;

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

        /// <summary>
        ///     solo song select builds its footer here, rooms override it and build their own
        /// </summary>
        [Feature(Feature.SongSelectFooter)]
        [HarmonyPatch(typeof(SongSelect), nameof(SongSelect.CreateFooterButtons))]
        internal static class SongSelectFooterInjection
        {
            [HarmonyPostfix]
            private static void postfix(ref IReadOnlyList<ScreenFooterButton> __result)
            {
                __result = FeatureRegistry.Run(Feature.SongSelectFooter, __result, buttons => [.. buttons, .. UiHooks.FooterButtons()]);
            }
        }

        /// <summary>
        ///     the toolbar is built once, its right-hand row only exists as the user button's parent, which is set once it has loaded
        /// </summary>
        [Feature(Feature.Toolbar)]
        [HarmonyPatch(typeof(Toolbar), nameof(Toolbar.LoadComplete))]
        internal static class ToolbarInjection
        {
            [HarmonyPostfix]
            private static void postfix(Toolbar __instance)
            {
                FeatureRegistry.Run(Feature.Toolbar, () =>
                {
                    if (__instance.userButton.Parent is FillFlowContainer row)
                        UiHooks.PopulateToolbar(row, __instance.userButton);
                    else
                        LoaderLog.Info("The toolbar's layout changed, mod toolbar buttons can't be placed.");
                });
            }
        }

        /// <summary>
        ///     each panel builds its menu when it's right-clicked, song select's scores implement the getter explicitly so it has no name to use
        ///     the interface map finds every panel's own getter instead, a panel that stops having a menu fails this feature when it's applied
        /// </summary>
        [Feature(Feature.ContextMenus)]
        [HarmonyPatch]
        internal static class ContextMenuInjection
        {
            [HarmonyTargetMethods]
            internal static IEnumerable<MethodBase> Targets()
            {
                var getter = typeof(IHasContextMenu).GetProperty(nameof(IHasContextMenu.ContextMenuItems))!.GetMethod!;

                foreach (var panel in new[] { typeof(PanelBeatmap), typeof(PanelBeatmapStandalone), typeof(PanelBeatmapSet), typeof(BeatmapLeaderboardScore), typeof(LeaderboardScore) })
                {
                    var map = panel.GetInterfaceMap(typeof(IHasContextMenu));
                    yield return map.TargetMethods[Array.IndexOf(map.InterfaceMethods, getter)];
                }
            }

            [HarmonyPostfix]
            private static void postfix(object __instance, ref MenuItem[]? __result)
            {
                __result = FeatureRegistry.Run(Feature.ContextMenus, __result, items => ContextMenus.Extend(__instance, items));
            }
        }

        /// <summary>
        ///     the skin editor's toolbox lists what this returns, a null ruleset is the global list every ruleset's HUD shares
        /// </summary>
        [Feature(Feature.SkinComponents)]
        [HarmonyPatch(typeof(SerialisedDrawableInfo), nameof(SerialisedDrawableInfo.GetAllAvailableDrawables))]
        internal static class SkinComponentToolbox
        {
            [HarmonyPostfix]
            private static void postfix(RulesetInfo? ruleset, ref Type[] __result)
            {
                if (ruleset == null)
                    __result = FeatureRegistry.Run(Feature.SkinComponents, __result, SkinComponents.WithModComponents);
            }
        }

        /// <summary>
        ///     one unresolvable type fails a whole layout, so a mod that isn't loaded only costs its own elements
        /// </summary>
        [Feature(Feature.SkinComponents)]
        [HarmonyPatch(typeof(Skin), nameof(Skin.parseLayoutInfo))]
        internal static class SkinLayoutFilter
        {
            [HarmonyPrefix]
            // skins can be built off the update thread, so nothing realm backed is read here
            private static void prefix(ref string jsonContent)
            {
                jsonContent = FeatureRegistry.Run(Feature.SkinComponents, jsonContent, json =>
                {
                    string filtered = SkinComponents.Filter(json, out int leftOut);

                    if (leftOut > 0)
                        LoaderLog.Info($"Left {leftOut} component(s) out of a skin layout, their mod isn't loaded.");

                    return filtered;
                });
            }
        }

        /// <summary>
        ///     the current skin was parsed before mods loaded, so it's built again once they have
        /// </summary>
        [Feature(Feature.SkinComponents)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class SkinComponentsReady
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance) => FeatureRegistry.Run(Feature.SkinComponents, () => SkinComponents.Attach(__instance.SkinManager));
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
                    var layer = new InputlessContainer
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
