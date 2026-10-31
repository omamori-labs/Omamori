using System;
using osu.Framework.Graphics;

namespace Omamori.API
{
    /// <summary>
    ///     mod ui inj, called when target is created
    /// </summary>
    public interface IUiHooks
    {
        void AddToMainMenu(Func<Drawable> factory);

        void AddGlobalOverlay(Func<Drawable> factory);

        /// <summary>
        ///     on top of song select, solo and multiplayer
        /// </summary>
        void AddToSongSelect(Func<Drawable> factory);

        /// <summary>
        ///     on top of the results screen after a play or when viewing a score
        /// </summary>
        void AddToResults(Func<Drawable> factory);

        /// <summary>
        ///     in the gameplay HUD, fades out with it when the player hides the HUD, gets no mouse or keyboard input
        /// </summary>
        void AddToGameplayHud(Func<Drawable> factory);
    }
}
