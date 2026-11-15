using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Omamori.API;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Replays.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Menu;
using osu.Game.Screens.OnlinePlay.DailyChallenge;
using osu.Game.Screens.OnlinePlay.Multiplayer;
using osu.Game.Screens.OnlinePlay.Multiplayer.Spectate;
using osu.Game.Screens.OnlinePlay.Playlists;
using osu.Game.Screens.Play;
using osu.Game.Users;

namespace Omamori.Core.Details
{
    /// <summary>
    ///     turns lazer's objects into the read-only copies mods get
    ///     null checks on realm objects use patterns, == needs realm's operators
    /// </summary>
    internal static class GameDetails
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

        /// <summary>spectators before replays, a spectator player is a replay player underneath</summary>
        public static PlayKind KindOf(Type player) =>
            typeof(SoloPlayer).IsAssignableFrom(player) ? PlayKind.Solo
            : typeof(PlaylistsPlayer).IsAssignableFrom(player) || typeof(DailyChallengePlayer).IsAssignableFrom(player) ? PlayKind.Playlist
            : typeof(MultiplayerPlayer).IsAssignableFrom(player) ? PlayKind.Multiplayer
            : typeof(SpectatorPlayer).IsAssignableFrom(player) || typeof(MultiSpectatorPlayer).IsAssignableFrom(player) ? PlayKind.Spectator
            : typeof(ReplayPlayer).IsAssignableFrom(player) ? PlayKind.Replay
            : PlayKind.Other;

        public static BeatmapDetails Beatmap(BeatmapInfo beatmap) => new BeatmapDetails
        {
            Title = beatmap.Metadata.Title,
            Artist = beatmap.Metadata.Artist,
            Creator = beatmap.Metadata.Author is { } author ? author.Username : string.Empty,
            DifficultyName = beatmap.DifficultyName,
            StarRating = beatmap.StarRating >= 0 ? beatmap.StarRating : null,
            Bpm = beatmap.BPM,
            Length = Time(beatmap.Length),
            OnlineId = beatmap.OnlineID > 0 ? beatmap.OnlineID : null,
            SetOnlineId = beatmap.BeatmapSet is { OnlineID: > 0 } set ? set.OnlineID : null,
            Ruleset = beatmap.Ruleset is { } ruleset ? ruleset.ShortName : string.Empty,
        };

        public static BeatmapSetDetails BeatmapSet(BeatmapSetInfo set) => new BeatmapSetDetails
        {
            Title = set.Metadata.Title,
            Artist = set.Metadata.Artist,
            Creator = set.Metadata.Author is { } author ? author.Username : string.Empty,
            OnlineId = set.OnlineID > 0 ? set.OnlineID : null,
            DifficultyCount = set.Beatmaps.Count,
        };

        public static BeatmapContent Content(IBeatmap playable, BeatmapInfo beatmap, RulesetInfo ruleset, IReadOnlyList<Mod> mods) => new BeatmapContent
        {
            Beatmap = Beatmap(beatmap),
            Ruleset = ruleset.ShortName,
            Mods = Mods(mods),
            HitObjects = [.. playable.HitObjects.Select(HitObjectOf)],
            TimingPoints = [.. playable.ControlPointInfo.TimingPoints.Select(t => new TimingPointDetails { Time = Time(t.Time), BeatLength = Time(t.BeatLength), Bpm = t.BPM, BeatsPerBar = t.TimeSignature.Numerator })],
            Breaks = [.. playable.Breaks.Select(b => new BreakDetails { StartTime = Time(b.StartTime), EndTime = Time(b.EndTime) })],
        };

        public static HitObjectDetails HitObjectOf(HitObject hit) => new HitObjectDetails
        {
            StartTime = Time(hit.StartTime),
            EndTime = Time(hit.GetEndTime()),
            Type = hit.GetType().Name,
            X = hit is IHasXPosition x ? x.X : null,
            Y = hit is IHasYPosition y ? y.Y : null,
            IsNewCombo = hit is IHasCombo { NewCombo: true },
        };

        public static ScoreDetails Score(ScoreInfo score) => new ScoreDetails
        {
            TotalScore = score.TotalScore,
            Accuracy = score.Accuracy,
            Grade = ByName(score.Rank, ScoreGrade.F),
            MaxCombo = score.MaxCombo,
            Pp = score.PP,
            IsPassed = score.Passed,
            Date = score.Date,
            Ruleset = score.Ruleset is { } ruleset ? ruleset.ShortName : string.Empty,
            Mods = [.. score.APIMods.Select(ModOf)],
            Beatmap = score.BeatmapInfo is { } beatmap ? Beatmap(beatmap) : null,
        };

        public static IReadOnlyList<HitDetails> Hits(ScoreInfo score) => [.. score.HitEvents.Select(Hit)];

        public static HitDetails Hit(HitEvent hit) => new HitDetails
        {
            Time = Time(hit.HitObject.StartTime),
            Offset = Time(hit.TimeOffset),
            Result = hit.Result.ToString(),
            X = hit.Position?.X,
            Y = hit.Position?.Y,
        };

        public static LivePlayState PlayState(ScoreProcessor score, HealthProcessor health) => new LivePlayState
        {
            TotalScore = score.TotalScore.Value,
            Accuracy = score.Accuracy.Value,
            Combo = score.Combo.Value,
            MaxCombo = score.HighestCombo.Value,
            Health = health.Health.Value,
            Grade = ByName(score.Rank.Value, ScoreGrade.F),
            JudgedCount = score.JudgedHits,
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
                             .Select(f => new ReplayFrameDetails { Time = Time(f.Time), X = f.MouseX ?? 0, Y = f.MouseY ?? 0, Buttons = (ReplayButtons)f.ButtonState }),
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

        public static IReadOnlyList<ModDetails> Mods(IReadOnlyList<Mod>? mods) => [.. (mods ?? []).Select(m => ModOf(new APIMod(m)))];

        /// <summary>
        ///     the form scores store, so a live mod and a saved one come out the same, and a score's mods don't need its ruleset
        /// </summary>
        public static ModDetails ModOf(APIMod mod) => new ModDetails
        {
            Acronym = mod.Acronym,
            Settings = mod.Settings.ToDictionary(s => s.Key, s => SettingText(s.Value)),
        };

        /// <summary>a saved score's settings come back from json, where an option is its number, so a live option is written as one too</summary>
        public static string SettingText(object? value) => value switch
        {
            null => string.Empty,
            bool b => b ? "true" : "false",
            Enum e => Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };

        /// <summary>
        ///     lazer's enum value as the api enum with the same name, so a value lazer adds later becomes <paramref name="fallback"/>
        /// </summary>
        public static T ByName<T>(Enum value, T fallback)
            where T : struct, Enum => Enum.TryParse(value.ToString(), out T result) && Enum.IsDefined(result) ? result : fallback;

        /// <summary>lazer's milliseconds, through ticks so fractions of a millisecond survive</summary>
        public static TimeSpan Time(double milliseconds) => TimeSpan.FromTicks((long)Math.Round(milliseconds * TimeSpan.TicksPerMillisecond));
    }
}
