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

        /// <summary><see cref="Hook.PlayEvents"/>, true when the player pauses, false when they resume</summary>
        void OnPlayPaused(Action<bool> handler);

        /// <summary><see cref="Hook.BeatmapEvents"/>, the selected mods as acronyms</summary>
        void OnModsChanged(Action<IReadOnlyList<string>> handler);

        /// <summary><see cref="Hook.LifecycleEvents"/>, the game is closing, save anything you need to now</summary>
        void OnGameExiting(Action handler);

        /// <summary><see cref="Hook.LifecycleEvents"/>, also raised once when the game starts</summary>
        void OnWindowChanged(Action<WindowDetails> handler);

        /// <summary><see cref="Hook.MusicEvents"/>, the song the music player moved to</summary>
        void OnTrackChanged(Action<BeatmapDetails> handler);

        /// <summary><see cref="Hook.MusicEvents"/>, true when music starts playing, false when it stops or pauses</summary>
        void OnMusicPlayingChanged(Action<bool> handler);

        /// <summary><see cref="Hook.UserEvents"/>, logging in or out, also raised once when the game starts</summary>
        void OnUserChanged(Action<UserDetails> handler);

        /// <summary><see cref="Hook.UserEvents"/>, the player's pp, rank and so on after the server processes a play</summary>
        void OnStatisticsUpdated(Action<StatisticsUpdate> handler);

        /// <summary><see cref="Hook.SkinEvents"/>, also raised once when the game starts</summary>
        void OnSkinChanged(Action<SkinDetails> handler);

        /// <summary><see cref="Hook.LibraryEvents"/>, a score was saved, from a play or an import</summary>
        void OnScoreSaved(Action<ScoreDetails> handler);

        /// <summary><see cref="Hook.LibraryEvents"/></summary>
        void OnBeatmapSetAdded(Action<BeatmapSetDetails> handler);

        /// <summary><see cref="Hook.LibraryEvents"/></summary>
        void OnBeatmapSetRemoved(Action<BeatmapSetDetails> handler);

        /// <summary><see cref="Hook.OverlayEvents"/>, settings, chat, beatmap listing and the other full overlays</summary>
        void OnOverlayChanged(Action<OverlayChange> handler);

        /// <summary><see cref="Hook.MultiplayerEvents"/>, joining, leaving or the room's settings changing</summary>
        void OnRoomChanged(Action<RoomChange> handler);
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

        /// <summary>every judgement, only when <see cref="Completed"/>, for hit error graphs and the like</summary>
        public IReadOnlyList<HitDetails> Hits { get; init; } = [];

        /// <summary>the replay, only when <see cref="Completed"/>, for cursor paths and key visualisers after the map</summary>
        public IReadOnlyList<ReplayFrameDetails> Replay { get; init; } = [];
    }

    public sealed record HitDetails
    {
        /// <summary>when the object was meant to be hit, in ms from the start of the map</summary>
        public double Time { get; init; }

        /// <summary>how early (negative) or late the hit was, in ms</summary>
        public double Offset { get; init; }

        /// <summary>Great, Ok, Meh, Miss and so on</summary>
        public string Result { get; init; } = string.Empty;

        /// <summary>where it was hit relative to the object, for rulesets with positions</summary>
        public float? X { get; init; }

        public float? Y { get; init; }
    }

    /// <summary>
    ///     the same frames a .osr file holds, X and Y are playfield positions in osu!, other rulesets pack their own data into them
    /// </summary>
    public sealed record ReplayFrameDetails
    {
        public double Time { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public ReplayButtons Buttons { get; init; }
    }

    [Flags]
    public enum ReplayButtons
    {
        None = 0,
        Left1 = 1,
        Right1 = 2,
        Left2 = 4,
        Right2 = 8,
        Smoke = 16,
    }

    public sealed record WindowDetails
    {
        public bool Focused { get; init; }
        public bool Fullscreen { get; init; }
    }

    public sealed record UserDetails
    {
        public bool LoggedIn { get; init; }

        /// <summary>null when not logged in</summary>
        public int? Id { get; init; }

        public string Username { get; init; } = string.Empty;

        /// <summary>two letters like JP, empty when unknown</summary>
        public string Country { get; init; } = string.Empty;
    }

    public sealed record StatisticsUpdate
    {
        public string Ruleset { get; init; } = string.Empty;

        /// <summary>null the first time statistics arrive this session</summary>
        public UserStatisticsDetails? Before { get; init; }

        public UserStatisticsDetails After { get; init; } = new UserStatisticsDetails();
    }

    public sealed record UserStatisticsDetails
    {
        public double? Pp { get; init; }
        public int? GlobalRank { get; init; }
        public int? CountryRank { get; init; }

        /// <summary>0 to 1</summary>
        public double Accuracy { get; init; }

        public int PlayCount { get; init; }
        public long RankedScore { get; init; }
    }

    public sealed record SkinDetails
    {
        public string Name { get; init; } = string.Empty;
        public string Creator { get; init; } = string.Empty;
    }

    public sealed record BeatmapSetDetails
    {
        public string Title { get; init; } = string.Empty;
        public string Artist { get; init; } = string.Empty;
        public string Creator { get; init; } = string.Empty;

        /// <summary>null for sets that aren't on the website</summary>
        public int? OnlineId { get; init; }

        public int Difficulties { get; init; }
    }

    public enum GameOverlay
    {
        Other,
        Settings,
        Notifications,
        Chat,
        BeatmapListing,
        BeatmapInfo,
        Dashboard,
        News,
        Changelog,
        Wiki,
        UserProfile,
    }

    public sealed record OverlayChange
    {
        public GameOverlay Overlay { get; init; }
        public bool Visible { get; init; }
    }

    public enum RoomChangeKind
    {
        Joined,
        Left,
        Updated,
    }

    public sealed record RoomChange
    {
        public RoomChangeKind Kind { get; init; }

        /// <summary>the room after the change, the one just left for <see cref="RoomChangeKind.Left"/></summary>
        public RoomDetails Room { get; init; } = new RoomDetails();
    }

    public sealed record RoomDetails
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;

        /// <summary>HeadToHead, TeamVersus and so on</summary>
        public string MatchType { get; init; } = string.Empty;

        /// <summary>Open, WaitingForLoad, Playing, Closed</summary>
        public string State { get; init; } = string.Empty;

        public int Users { get; init; }
        public bool HasPassword { get; init; }
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

        public string Ruleset { get; init; } = string.Empty;

        public IReadOnlyList<string> Mods { get; init; } = [];

        /// <summary>null when the beatmap isn't in the library</summary>
        public BeatmapDetails? Beatmap { get; init; }
    }
}
