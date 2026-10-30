using System;
using System.Collections.Generic;

namespace Omamori.API
{
    /// <summary>
    ///     read-only, mods get copies of what happened and can't change it
    ///     handlers run on the update thread, keep them quick, one that throws turns its mod off
    /// </summary>
    public interface IGameEvents
    {
        /// <summary><see cref="Hook.ScreenEvents"/></summary>
        void OnScreenChanged(Action<ScreenChange> handler);

        /// <summary><see cref="Hook.BeatmapEvents"/>, also raised once with the current beatmap when the game starts</summary>
        void OnBeatmapChanged(Action<BeatmapDetails> handler);

        /// <summary><see cref="Hook.BeatmapEvents"/>, also raised once with the current ruleset when the game starts</summary>
        void OnRulesetChanged(Action<RulesetDetails> handler);

        /// <summary><see cref="Hook.PlayEvents"/>, includes replays, check <see cref="PlayDetails.IsReplay"/></summary>
        void OnPlayStarted(Action<PlayDetails> handler);

        /// <summary><see cref="Hook.PlayEvents"/>, the final result only, never live gameplay state</summary>
        void OnPlayEnded(Action<PlayResult> handler);
    }

    public enum GameScreen
    {
        Other,
        MainMenu,
        SongSelect,
        Gameplay,
        Results,
        Editor,
        Multiplayer,
        Playlists,
    }

    public sealed record ScreenChange
    {
        public GameScreen Screen { get; init; }

        public GameScreen Previous { get; init; }

        /// <summary>lazer's own class name, not stable across osu! versions, for logging</summary>
        public string ScreenName { get; init; } = string.Empty;
    }

    public sealed record BeatmapDetails
    {
        public string Title { get; init; } = string.Empty;
        public string Artist { get; init; } = string.Empty;

        /// <summary>the mapper</summary>
        public string Creator { get; init; } = string.Empty;

        public string Difficulty { get; init; } = string.Empty;

        /// <summary>-1 when it hasn't been calculated yet</summary>
        public double StarRating { get; init; }

        public double Bpm { get; init; }
        public TimeSpan Length { get; init; }

        /// <summary>null for beatmaps that aren't on the website</summary>
        public int? OnlineId { get; init; }

        public string Ruleset { get; init; } = string.Empty;
    }

    public sealed record RulesetDetails
    {
        /// <summary>osu, taiko, fruits, mania, or a custom ruleset's short name</summary>
        public string ShortName { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;
    }

    public sealed record PlayDetails
    {
        public BeatmapDetails Beatmap { get; init; } = new BeatmapDetails();

        public string Ruleset { get; init; } = string.Empty;

        /// <summary>acronyms like HD, DT</summary>
        public IReadOnlyList<string> Mods { get; init; } = [];

        /// <summary>watching a replay, autoplay or spectating, not the player playing</summary>
        public bool IsReplay { get; init; }
    }

    public sealed record PlayResult
    {
        public PlayDetails Play { get; init; } = new PlayDetails();

        /// <summary>false when the player quit, failed and left, or retried</summary>
        public bool Completed { get; init; }

        /// <summary>only when <see cref="Completed"/></summary>
        public ScoreDetails? Score { get; init; }
    }

    public sealed record ScoreDetails
    {
        public long TotalScore { get; init; }

        /// <summary>0 to 1</summary>
        public double Accuracy { get; init; }

        /// <summary>D, C, B, A, S, SH, X, XH, or F for a fail</summary>
        public string Rank { get; init; } = string.Empty;

        public int MaxCombo { get; init; }

        /// <summary>null until the server works it out, or when it doesn't give pp</summary>
        public double? Pp { get; init; }

        public bool Passed { get; init; }

        public DateTimeOffset Date { get; init; }
    }
}
