namespace Omamori.API
{
    /// <summary>
    ///     integration points with the game, declared in the manifest's hooks
    /// </summary>
    public enum Hook
    {
        /// <summary><see cref="IUiHooks.AddToMainMenu"/></summary>
        MainMenu,

        /// <summary><see cref="IUiHooks.AddGlobalOverlay"/></summary>
        GlobalOverlay,

        /// <summary><see cref="IGameEvents.OnScreenChanged"/></summary>
        ScreenEvents,

        /// <summary><see cref="IGameEvents.OnBeatmapChanged"/>, <see cref="IGameEvents.OnRulesetChanged"/> and <see cref="IGameEvents.OnModsChanged"/></summary>
        BeatmapEvents,

        /// <summary><see cref="IGameEvents.OnPlayStarted"/>, <see cref="IGameEvents.OnPlayEnded"/> and <see cref="IGameEvents.OnPlayPaused"/></summary>
        PlayEvents,

        /// <summary><see cref="IGameEvents.OnGameExiting"/> and <see cref="IGameEvents.OnWindowChanged"/></summary>
        LifecycleEvents,

        /// <summary><see cref="IGameEvents.OnTrackChanged"/> and <see cref="IGameEvents.OnMusicPlayingChanged"/></summary>
        MusicEvents,

        /// <summary><see cref="IGameEvents.OnUserChanged"/> and <see cref="IGameEvents.OnStatisticsUpdated"/></summary>
        UserEvents,

        /// <summary><see cref="IGameEvents.OnSkinChanged"/></summary>
        SkinEvents,

        /// <summary><see cref="IGameEvents.OnScoreSaved"/>, <see cref="IGameEvents.OnBeatmapSetAdded"/> and <see cref="IGameEvents.OnBeatmapSetRemoved"/></summary>
        LibraryEvents,

        /// <summary><see cref="IGameEvents.OnOverlayChanged"/></summary>
        OverlayEvents,

        /// <summary><see cref="IGameEvents.OnRoomChanged"/></summary>
        MultiplayerEvents,
    }
}
