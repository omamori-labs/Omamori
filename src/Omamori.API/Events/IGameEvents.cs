using System;
using System.Collections.Generic;

namespace Omamori.API
{
    /// <summary>
    ///     read-only, mods get copies of what happened and can't change it
    ///     handlers run on the update thread, keep them quick, one that throws turns its mod off
    ///     register from <see cref="IMod.Load"/>, every event about state then tells the mod where it starts, as each one says
    ///     disposing what registering returns stops that handler, there's no need to when the mod is turned off
    /// </summary>
    public interface IGameEvents
    {
        /// <summary><see cref="Hook.ScreenEvents"/>, raised for every screen from the intro on, so the last one raised is the current screen</summary>
        IDisposable OnScreenChanged(Action<ScreenChange> handler);

        /// <summary><see cref="Hook.BeatmapEvents"/>, also raised once with the current beatmap when the game starts</summary>
        IDisposable OnBeatmapChanged(Action<BeatmapDetails> handler);

        /// <summary><see cref="Hook.BeatmapEvents"/>, also raised once with the current ruleset when the game starts</summary>
        IDisposable OnRulesetChanged(Action<RulesetDetails> handler);

        /// <summary><see cref="Hook.PlayEvents"/>, includes replays, check <see cref="PlayDetails.IsReplay"/></summary>
        IDisposable OnPlayStarted(Action<PlayDetails> handler);

        /// <summary><see cref="Hook.PlayEvents"/>, the final result, live state is <see cref="OnPlayStateChanged"/></summary>
        IDisposable OnPlayEnded(Action<PlayResult> handler);

        /// <summary><see cref="Hook.PlayEvents"/>, true when the player pauses, false when they resume, every play starts unpaused</summary>
        IDisposable OnPlayPauseChanged(Action<bool> handler);

        /// <summary><see cref="Hook.BeatmapEvents"/>, the selected mods, raised when a setting changes too, also raised once when the game starts</summary>
        IDisposable OnModsChanged(Action<IReadOnlyList<ModDetails>> handler);

        /// <summary><see cref="Hook.LifecycleEvents"/>, the game is closing, save anything you need to now</summary>
        IDisposable OnGameExiting(Action handler);

        /// <summary><see cref="Hook.LifecycleEvents"/>, also raised once when the game starts</summary>
        IDisposable OnWindowChanged(Action<WindowDetails> handler);

        /// <summary><see cref="Hook.MusicEvents"/>, the song the music player moved to</summary>
        IDisposable OnTrackChanged(Action<BeatmapDetails> handler);

        /// <summary><see cref="Hook.MusicEvents"/>, true when music starts playing, false when it stops or pauses, the music starts stopped</summary>
        IDisposable OnMusicPlayingChanged(Action<bool> handler);

        /// <summary><see cref="Hook.UserEvents"/>, logging in or out, also raised once when the game starts</summary>
        IDisposable OnUserChanged(Action<UserDetails> handler);

        /// <summary><see cref="Hook.UserEvents"/>, the player's pp, rank and so on after the server processes a play</summary>
        IDisposable OnStatisticsUpdated(Action<StatisticsUpdate> handler);

        /// <summary><see cref="Hook.SkinEvents"/>, also raised once when the game starts</summary>
        IDisposable OnSkinChanged(Action<SkinDetails> handler);

        /// <summary><see cref="Hook.LibraryEvents"/>, a score was saved, from a play or an import</summary>
        IDisposable OnScoreSaved(Action<ScoreDetails> handler);

        /// <summary><see cref="Hook.LibraryEvents"/></summary>
        IDisposable OnBeatmapSetAdded(Action<BeatmapSetDetails> handler);

        /// <summary><see cref="Hook.LibraryEvents"/></summary>
        IDisposable OnBeatmapSetRemoved(Action<BeatmapSetDetails> handler);

        /// <summary><see cref="Hook.OverlayEvents"/>, settings, chat, beatmap listing and the other full overlays, they all start hidden</summary>
        IDisposable OnOverlayChanged(Action<OverlayChange> handler);

        /// <summary><see cref="Hook.MultiplayerEvents"/>, joining, leaving or the room's settings changing, the game starts outside any room</summary>
        IDisposable OnRoomChanged(Action<RoomChange> handler);

        /// <summary><see cref="Hook.LivePlayEvents"/>, every judgement as it happens during a play, the same ones <see cref="PlayResult.Hits"/> ends up with</summary>
        IDisposable OnHit(Action<HitDetails> handler);

        /// <summary><see cref="Hook.LivePlayEvents"/>, score, combo, health and so on, raised when a play loads and after every judgement</summary>
        IDisposable OnPlayStateChanged(Action<LivePlayState> handler);
    }
}
