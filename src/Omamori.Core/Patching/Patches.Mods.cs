using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Omamori.Core.Features;
using Omamori.Core.Mods;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace Omamori.Core.Patching
{
    internal static partial class Patches
    {
        /// <summary>
        ///     every ruleset's mod list comes through here, mod select, score decoding and multiplayer included
        /// </summary>
        [Feature(Feature.GameMods)]
        [HarmonyPatch(typeof(Ruleset), nameof(Ruleset.CreateAllMods))]
        internal static class GameModsInjection
        {
            [HarmonyPostfix]
            private static void postfix(Ruleset __instance, ref IEnumerable<Mod> __result)
            {
                __result = FeatureRegistry.Run(Feature.GameMods, __result, mods => mods.Concat(GameMods.For(__instance)));
            }
        }
    }
}
