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
}
