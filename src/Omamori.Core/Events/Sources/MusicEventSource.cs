using osu.Game;
using osu.Game.Beatmaps;

namespace Omamori.Core
{
    internal static class MusicEventSource
    {
        public static void Watch(OsuGame game)
        {
            game.MusicController.TrackChanged += (beatmap, _) => FeatureRegistry.Run(Feature.MusicEvents, () =>
            {
                if (beatmap is not DummyWorkingBeatmap)
                    GameEvents.Raise(GameEvent.TrackChanged, EventDetails.Beatmap(beatmap.BeatmapInfo));
            });

            game.Add(new MusicPlayingWatcher(game.MusicController));
        }
    }
}
