using Omamori.API;
using Omamori.Core.Features;
using osu.Framework.Bindables;
using osu.Game;
using osu.Game.Scoring;
using osu.Game.Screens.Play;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     a play starts when a player screen is pushed and ends when it exits, or completes when it pushes results
    /// </summary>
    internal static class PlayEventSource
    {
        private static readonly PlayTracker plays = new PlayTracker();
        private static IBindable<bool>? paused;

        public static void Watch(OsuGame game)
        {
            game.ScreenStack.ScreenPushed += (last, next) => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (last is Player finished && next is osu.Game.Screens.Ranking.ResultsScreen results && results.Score is { } score)
                    end(finished, score);

                if (next is Player player)
                    start(player);
            });

            // results exiting resumes the player, which isn't valid for resume and exits too, the tracker keeps that from ending twice
            game.ScreenStack.ScreenExited += (last, _) => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (last is Player player)
                    end(player, null);
            });
        }

        private static void start(Player player)
        {
            var play = new PlayDetails
            {
                Beatmap = EventDetails.Beatmap(player.Beatmap.Value.BeatmapInfo),
                Ruleset = player.Ruleset.Value is { } ruleset ? ruleset.ShortName : string.Empty,
                Mods = EventDetails.Acronyms(player.Mods.Value),
                IsReplay = player is ReplayPlayer or SpectatorPlayer,
            };

            if (plays.Start(play) is { } interrupted)
                raiseEnded(interrupted, player, null);

            GameEvents.Raise(GameEvent.PlayStarted, play);

            // the clock only exists once the player has loaded
            if (player.GameplayClockContainer != null)
                watchPause(player);
            else
                player.OnLoadComplete += _ => watchPause(player);
        }

        private static void watchPause(Player player)
        {
            var tracker = new PauseTracker();

            paused = player.GameplayClockContainer.IsPaused.GetBoundCopy();
            paused.BindValueChanged(e => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (tracker.IsPlayerPause(e.NewValue, player.GameplayState.HasCompleted))
                    GameEvents.Raise(GameEvent.PlayPaused, e.NewValue);
            }));
        }

        private static void end(Player player, ScoreInfo? score)
        {
            if (plays.End() is { } play)
                raiseEnded(play, player, score);
        }

        private static void raiseEnded(PlayDetails play, Player player, ScoreInfo? score)
        {
            paused?.UnbindAll();
            paused = null;

            // the replay is the costly part, only convert it for someone
            bool detailed = score is not null && GameEvents.HasHandlers(GameEvent.PlayEnded);

            GameEvents.Raise(GameEvent.PlayEnded, new PlayResult
            {
                Play = play,
                Completed = score is not null,
                Score = score is null ? null : EventDetails.Score(score),
                Hits = detailed ? EventDetails.Hits(score!) : [],
                Replay = detailed ? EventDetails.Replay(player) : [],
            });
        }
    }
}
