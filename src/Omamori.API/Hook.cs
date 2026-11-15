namespace Omamori.API
{
    /// <summary>
    ///     integration points with the game, declared in the manifest's hooks
    /// </summary>
    public enum Hook
    {
        /// <summary><see cref="IModUi.AddToMainMenu"/></summary>
        MainMenu,

        /// <summary><see cref="IModUi.AddGlobalOverlay"/></summary>
        GlobalOverlay,

        /// <summary><see cref="IGameEvents.OnScreenChanged"/></summary>
        ScreenEvents,

        /// <summary><see cref="IGameEvents.OnBeatmapChanged"/>, <see cref="IGameEvents.OnRulesetChanged"/> and <see cref="IGameEvents.OnModsChanged"/></summary>
        BeatmapEvents,

        /// <summary><see cref="IGameEvents.OnPlayStarted"/>, <see cref="IGameEvents.OnPlayEnded"/> and <see cref="IGameEvents.OnPlayPauseChanged"/></summary>
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

        /// <summary><see cref="IModUi.AddToSongSelect"/></summary>
        SongSelect,

        /// <summary><see cref="IModUi.AddToResults"/></summary>
        Results,

        /// <summary><see cref="IModUi.AddToGameplayHud"/></summary>
        GameplayHud,

        /// <summary><see cref="IGameEvents.OnHit"/> and <see cref="IGameEvents.OnPlayStateChanged"/></summary>
        LivePlayEvents,

        /// <summary><see cref="IModContext.Notifications"/></summary>
        Notifications,

        /// <summary><see cref="IModInput.OnPressed"/> and <see cref="IModInput.OnReleased"/></summary>
        Input,

        /// <summary><see cref="IModContext.Presence"/></summary>
        Presence,

        /// <summary><see cref="IGameMods.Add{T}"/></summary>
        GameMods,

        /// <summary><see cref="IGamePlays.OnStarting"/></summary>
        PlayStarting,

        /// <summary><see cref="IModUi.AddToSongSelectFooter"/></summary>
        SongSelectFooter,

        /// <summary><see cref="IGamePlays.Running"/> and <see cref="IGamePlays.OnFailing"/></summary>
        PlayControl,

        /// <summary><see cref="IModContext.GameNavigation"/></summary>
        Navigation,

        /// <summary><see cref="IModUi.AddToolbarButton"/></summary>
        Toolbar,

        /// <summary><see cref="IModUi.AddBeatmapMenuItem"/>, <see cref="IModUi.AddBeatmapSetMenuItem"/> and <see cref="IModUi.AddScoreMenuItem"/></summary>
        ContextMenus,

        /// <summary><see cref="IModUi.AddSkinComponent{T}"/></summary>
        SkinComponents,

        /// <summary><see cref="IModContext.GameBeatmaps"/></summary>
        Beatmaps,

        /// <summary><see cref="IModContext.Audio"/></summary>
        Audio,
    }
}
