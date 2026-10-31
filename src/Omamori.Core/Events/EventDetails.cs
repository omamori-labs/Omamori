using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Replays.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Play;
using osu.Game.Users;

namespace Omamori.Core.Events
{
    /// <summary>
    ///     turns lazer's objects into the read-only copies mods get
    ///     null checks on realm objects use patterns, == needs realm's operators
    /// </summary>
    internal static class EventDetails
    {
        public static GameScreen Screen(IScreen? screen) => screen switch
        {
            MainMenu => GameScreen.MainMenu,
            osu.Game.Screens.Select.SongSelect => GameScreen.SongSelect,
            Player => GameScreen.Gameplay,
            osu.Game.Screens.Ranking.ResultsScreen => GameScreen.Results,
            osu.Game.Screens.Edit.Editor => GameScreen.Editor,
            osu.Game.Screens.OnlinePlay.Multiplayer.Multiplayer => GameScreen.Multiplayer,
            osu.Game.Screens.OnlinePlay.Playlists.Playlists => GameScreen.Playlists,
            _ => GameScreen.Other,
        };

        public static BeatmapDetails Beatmap(BeatmapInfo beatmap) => new BeatmapDetails
        {
            Title = beatmap.Metadata.Title,
            Artist = beatmap.Metadata.Artist,
            Creator = beatmap.Metadata.Author is { } author ? author.Username : string.Empty,
            Difficulty = beatmap.DifficultyName,
            StarRating = beatmap.StarRating,
            Bpm = beatmap.BPM,
            Length = TimeSpan.FromMilliseconds(beatmap.Length),
            OnlineId = beatmap.OnlineID > 0 ? beatmap.OnlineID : null,
            Ruleset = beatmap.Ruleset is { } ruleset ? ruleset.ShortName : string.Empty,
        };

        public static BeatmapSetDetails BeatmapSet(BeatmapSetInfo set) => new BeatmapSetDetails
        {
            Title = set.Metadata.Title,
            Artist = set.Metadata.Artist,
            Creator = set.Metadata.Author is { } author ? author.Username : string.Empty,
            OnlineId = set.OnlineID > 0 ? set.OnlineID : null,
            Difficulties = set.Beatmaps.Count,
        };

        public static ScoreDetails Score(ScoreInfo score) => new ScoreDetails
        {
            TotalScore = score.TotalScore,
            Accuracy = score.Accuracy,
            Rank = score.Rank.ToString(),
            MaxCombo = score.MaxCombo,
            Pp = score.PP,
            Passed = score.Passed,
            Date = score.Date,
            Ruleset = score.Ruleset is { } ruleset ? ruleset.ShortName : string.Empty,
            // api mods don't need the ruleset instantiated
            Mods = [.. score.APIMods.Select(m => m.Acronym)],
            Beatmap = score.BeatmapInfo is { } beatmap ? Beatmap(beatmap) : null,
        };

        public static IReadOnlyList<HitDetails> Hits(ScoreInfo score) => [.. score.HitEvents.Select(Hit)];

        public static HitDetails Hit(HitEvent hit) => new HitDetails
        {
            Time = hit.HitObject.StartTime,
            Offset = hit.TimeOffset,
            Result = hit.Result.ToString(),
            X = hit.Position?.X,
            Y = hit.Position?.Y,
        };

        public static LivePlayState PlayState(ScoreProcessor score, HealthProcessor health) => new LivePlayState
        {
            TotalScore = score.TotalScore.Value,
            Accuracy = score.Accuracy.Value,
            Combo = score.Combo.Value,
            HighestCombo = score.HighestCombo.Value,
            Health = health.Health.Value,
            Rank = score.Rank.Value.ToString(),
            Judged = score.JudgedHits,
        };

        /// <summary>
        ///     the frames a .osr holds, a custom ruleset's frames can fail to convert, the rest of the result is still useful then
        /// </summary>
        public static IReadOnlyList<ReplayFrameDetails> Replay(Player player)
        {
            try
            {
                var beatmap = player.GameplayState.Beatmap;

                return
                [
                    .. player.Score.Replay.Frames.OfType<IConvertibleReplayFrame>()
                             .Select(f => f.ToLegacy(beatmap))
                             .Select(f => new ReplayFrameDetails { Time = f.Time, X = f.MouseX ?? 0, Y = f.MouseY ?? 0, Buttons = (ReplayButtons)f.ButtonState }),
                ];
            }
            catch (Exception e)
            {
                LoaderLog.Error("Couldn't convert the replay for a play ended event.", e);
                return [];
            }
        }

        public static UserStatisticsDetails Statistics(UserStatistics statistics) => new UserStatisticsDetails
        {
            Pp = statistics.PP is { } pp ? (double)pp : null,
            GlobalRank = statistics.GlobalRank,
            CountryRank = statistics.CountryRank,
            Accuracy = statistics.Accuracy / 100,
            PlayCount = statistics.PlayCount,
            RankedScore = statistics.RankedScore,
        };

        public static IReadOnlyList<string> Acronyms(IReadOnlyList<Mod>? mods) => [.. (mods ?? []).Select(m => m.Acronym)];
    }
}
