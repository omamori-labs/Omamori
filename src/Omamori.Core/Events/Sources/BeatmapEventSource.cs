using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using osu.Game;
using osu.Game.Beatmaps;

namespace Omamori.Core
{
    /// <summary>
    ///     the global beatmap, ruleset and selected mods
    /// </summary>
    internal static class BeatmapEventSource
    {
        private static IReadOnlyList<string> lastMods = [];

        public static void Watch(OsuGame game)
        {
            game.Beatmap.BindValueChanged(e => FeatureRegistry.Run(Feature.BeatmapEvents, () =>
            {
                // song select swaps in a new working beatmap for the same map, RaiseChanged drops the repeat
                if (e.NewValue is not DummyWorkingBeatmap)
                    GameEvents.RaiseChanged(GameEvent.BeatmapChanged, EventDetails.Beatmap(e.NewValue.BeatmapInfo));
            }), true);

            game.Ruleset.BindValueChanged(e => FeatureRegistry.Run(Feature.BeatmapEvents, () =>
            {
                if (e.NewValue is { } ruleset)
                    GameEvents.RaiseChanged(GameEvent.RulesetChanged, new RulesetDetails { ShortName = ruleset.ShortName, Name = ruleset.Name });
            }), true);

            game.SelectedMods.BindValueChanged(e => FeatureRegistry.Run(Feature.BeatmapEvents, () =>
            {
                // lazer reorders the same mods, that isn't a change
                var mods = EventDetails.Acronyms(e.NewValue);
                if (mods.ToHashSet().SetEquals(lastMods))
                    return;

                lastMods = mods;
                GameEvents.Raise(GameEvent.ModsChanged, mods);
            }));
        }
    }
}
