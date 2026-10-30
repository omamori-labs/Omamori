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

    public static class ApiInfo
    {
        public const string VERSION = "0.1";
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
    }
}
