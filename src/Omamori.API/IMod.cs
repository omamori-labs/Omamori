using System;
using System.Collections.Generic;
using osu.Framework.Graphics;

namespace Omamori.API
{
    /// <summary>
    ///     creates an instance per mod, calls <see cref="Load"/> once at game launch
    /// </summary>
    public interface IMod
    {
        void Load(IModContext context);
    }

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

        /// <summary><see cref="IGameEvents.OnBeatmapChanged"/> and <see cref="IGameEvents.OnRulesetChanged"/></summary>
        BeatmapEvents,

        /// <summary><see cref="IGameEvents.OnPlayStarted"/> and <see cref="IGameEvents.OnPlayEnded"/></summary>
        PlayEvents,
    }

    public enum HookRequirement
    {
        /// <summary>mod is skipped if the hook isn't available</summary>
        Required,

        /// <summary>mod loads anyway, only what uses the hook is skipped</summary>
        Optional,
    }

    public interface IModLogger
    {
        void Info(string message);
        void Error(string message, Exception? exception = null);
    }

    /// <summary>
    ///     mod ui inj, called when target is created
    /// </summary>
    public interface IUiHooks
    {
        void AddToMainMenu(Func<Drawable> factory);

        void AddGlobalOverlay(Func<Drawable> factory);
    }

    /// <summary>
    ///     what a mod can do, set in its manifest
    /// </summary>
    public enum ModCapability
    {
        /// <summary>cosmetic only</summary>
        Cosmetic,

        /// <summary>info about game state</summary>
        Informational,
    }

    public sealed class ModManifest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>name of mods assembly</summary>
        public string Entry { get; set; } = string.Empty;

        /// <summary>The <see cref="ApiInfo.VERSION"/> the mod was built for</summary>
        public string ApiVersion { get; set; } = string.Empty;

        public List<ModCapability> Capabilities { get; set; } = [];

        /// <summary>hooks the mod uses, undeclared ones count as optional</summary>
        public Dictionary<Hook, HookRequirement> Hooks { get; set; } = [];

        public List<ModDependency> Dependencies { get; set; } = [];
    }

    public sealed class ModDependency
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>lowest accepted version, any version if empty</summary>
        public string MinVersion { get; set; } = string.Empty;

        /// <summary>
        ///     required deps that are missing or fail skip this mod
        ///     optional ones just load first when present, check with <see cref="IModContext.IsModLoaded"/>
        /// </summary>
        public bool Optional { get; set; }
    }
}
