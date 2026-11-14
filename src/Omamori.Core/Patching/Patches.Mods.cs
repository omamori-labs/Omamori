using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Omamori.Core.Features;
using Omamori.Core.Mods;
using osu.Framework.Bindables;
using osu.Game;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play;

namespace Omamori.Core.Patching
{
    internal static partial class Patches
    {
        /// <summary>
        ///     the full mod list behind acronym lookups, score decoding and the check a player makes before it loads
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

        /// <summary>
        ///     mod select shows the lists built here from each ruleset's GetModsFor, which CreateAllMods doesn't go through
        /// </summary>
        [Feature(Feature.GameMods)]
        [HarmonyPatch(typeof(OsuGameBase), nameof(OsuGameBase.onRulesetChanged))]
        internal static class GameModsInModSelect
        {
            [HarmonyPrefix]
            private static void prefix(OsuGameBase __instance, out IReadOnlyDictionary<ModType, IReadOnlyList<Mod>> __state) => __state = __instance.AvailableMods.Value;

            // a ruleset that fails to load leaves the old lists in place, those already have the mods
            [HarmonyPostfix]
            private static void postfix(OsuGameBase __instance, ValueChangedEvent<RulesetInfo> __0, IReadOnlyDictionary<ModType, IReadOnlyList<Mod>> __state)
            {
                FeatureRegistry.Run(Feature.GameMods, () =>
                {
                    if (!ReferenceEquals(__instance.AvailableMods.Value, __state) && __0.NewValue is { } ruleset)
                        addGameMods(__instance, ruleset);
                });
            }
        }

        /// <summary>
        ///     the first ruleset's lists are built before mods load, so they're redone once mods have
        /// </summary>
        [Feature(Feature.GameMods)]
        [HarmonyPatch(typeof(OsuGame), nameof(OsuGame.LoadComplete))]
        internal static class GameModsAfterLoading
        {
            [HarmonyPostfix]
            private static void postfix(OsuGame __instance)
            {
                FeatureRegistry.Run(Feature.GameMods, () =>
                {
                    if (__instance.Ruleset.Value is { Available: true } ruleset)
                        addGameMods(__instance, ruleset);
                });
            }
        }

        /// <summary>
        ///     osu! has never heard of a mod added by a mod, so a play using one is never submitted
        /// </summary>
        [Feature(Feature.GameMods)]
        [HarmonyPatch(typeof(SoloPlayer), nameof(SoloPlayer.CreateTokenRequest))]
        internal static class GameModsUnsubmitted
        {
            // false once another prefix has already kept the play from being submitted
            [HarmonyPrefix]
            private static bool prefix(SoloPlayer __instance, ref APIRequest<APIScoreToken>? __result, bool __runOriginal) =>
                __runOriginal && submits(GameMods.Unsubmitted(__instance.Mods.Value), ref __result);
        }

        private static void addGameMods(OsuGameBase game, RulesetInfo ruleset) =>
            game.AvailableMods.Value = GameMods.AddTo(game.AvailableMods.Value, ruleset.CreateInstance());
    }
}
