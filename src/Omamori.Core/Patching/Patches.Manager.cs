using HarmonyLib;
using Omamori.Core.Features;
using Omamori.Core.Manager;
using osu.Game;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings.Sections;

namespace Omamori.Core.Patching
{
    // omamori's own ui, the settings section and the mod manager
    internal static partial class Patches
    {
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

                    sections.Insert(maintenance < 0 ? sections.Count : maintenance, new OmamoriSettingsSection());
                });
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
    }
}
