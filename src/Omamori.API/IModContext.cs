namespace Omamori.API
{
    /// <summary>
    ///     everything a mod is allowed to touch, the Game ones reach into osu! and the rest belong to the mod
    /// </summary>
    public interface IModContext
    {
        /// <summary>a copy, changing it does nothing</summary>
        ModManifest Manifest { get; }

        /// <summary>the mod's install folder, replaced when it updates or redeploys</summary>
        string ModDirectory { get; }

        /// <summary>the mod's own folder for files it writes</summary>
        string DataDirectory { get; }

        IModLogger Log { get; }

        IModUi Ui { get; }

        IGameEvents GameEvents { get; }

        IModSettings Settings { get; }

        IModNotifications Notifications { get; }

        IModInput Input { get; }

        IModServices Services { get; }

        IModPresence Presence { get; }

        IGameMods GameMods { get; }

        IGamePlays GamePlays { get; }

        IGameNavigation GameNavigation { get; }

        IGameBeatmaps GameBeatmaps { get; }

        IModAudio Audio { get; }

        IGameMusic GameMusic { get; }

        /// <summary>
        ///     false when this osu! version doesn't support the hook, or it broke this session
        ///     registering for an unavailable hook is safe, it's just skipped
        /// </summary>
        bool IsHookAvailable(Hook hook);

        /// <summary>
        ///     whether another mod loaded, dependencies always load first so this is accurate inside <see cref="IMod.Load"/>
        /// </summary>
        bool IsModLoaded(string id);

        /// <summary>
        ///     opens a folder in the system's file browser, or shows a file selected in its folder
        ///     a file is never opened, false when the path doesn't exist
        /// </summary>
        bool ShowInFileBrowser(string path);
    }
}
