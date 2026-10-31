namespace Omamori.API
{
    /// <summary>
    ///     Everything a mod is allowed to touch
    /// </summary >
    public interface IModContext
    {
        ModManifest Manifest { get; }

        string ModDirectory { get; }

        IModLogger Log { get; }

        IUiHooks Ui { get; }

        IGameEvents Events { get; }

        IModSettings Settings { get; }

        IModNotifications Notifications { get; }

        IModInput Input { get; }

        IModServices Services { get; }

        /// <summary>
        ///     false when this osu! version doesn't support the hook, or it broke this session.
        ///     registering for an unavailable hook is safe, it's just skipped.
        /// </summary>
        bool IsHookAvailable(Hook hook);

        /// <summary>
        ///     whether another mod loaded, dependencies always load first so this is accurate inside <see cref="IMod.Load"/>
        /// </summary>
        bool IsModLoaded(string id);
    }
}
