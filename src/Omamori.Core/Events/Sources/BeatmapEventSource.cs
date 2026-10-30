using Omamori.API;
using Omamori.Core.Features;
using osu.Game;
using osu.Game.Beatmaps;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     the global beatmap, ruleset and selected mods
    /// </summary>
    internal static class BeatmapEventSource
    {
        private static readonly SelectedModsTracker selected_mods = new SelectedModsTracker();

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
                var mods = EventDetails.Acronyms(e.NewValue);

                if (selected_mods.Update(mods))
                    GameEvents.Raise(GameEvent.ModsChanged, mods);
            }));
        }
    }
}
