using Omamori.API;
using osu.Framework.Bindables;
using osu.Game;
using osu.Game.Scoring;
using osu.Game.Screens.Play;

namespace Omamori.Core
{
    /// <summary>
    ///     a play starts when a player screen is pushed and ends when it exits, or completes when it pushes results
    /// </summary>
    internal static class PlayEventSource
    {
        private static PlayDetails? activePlay;
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

            // results exiting resumes the player, which isn't valid for resume and exits too, activePlay keeps that from ending twice
            game.ScreenStack.ScreenExited += (last, _) => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (last is Player player)
                    end(player, null);
            });
        }

        private static void start(Player player)
        {
            if (activePlay != null)
                end(player, null);

            activePlay = new PlayDetails
            {
                Beatmap = EventDetails.Beatmap(player.Beatmap.Value.BeatmapInfo),
                Ruleset = player.Ruleset.Value is { } ruleset ? ruleset.ShortName : string.Empty,
                Mods = EventDetails.Acronyms(player.Mods.Value),
                IsReplay = player is ReplayPlayer or SpectatorPlayer,
            };

            GameEvents.Raise(GameEvent.PlayStarted, activePlay);

            // the clock only exists once the player has loaded
            if (player.GameplayClockContainer != null)
                watchPause(player);
            else
                player.OnLoadComplete += _ => watchPause(player);
        }

        // the clock starts paused and stops when the play ends, only pauses in between are reported
        private static void watchPause(Player player)
        {
            bool started = false;

            paused = player.GameplayClockContainer.IsPaused.GetBoundCopy();
            paused.BindValueChanged(e => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (player.GameplayState.HasCompleted)
                    return;

                if (!started)
                {
                    started = !e.NewValue;
                    return;
                }

                GameEvents.Raise(GameEvent.PlayPaused, e.NewValue);
            }));
        }

        private static void end(Player player, ScoreInfo? score)
        {
            if (activePlay is not { } play)
                return;

            activePlay = null;
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
