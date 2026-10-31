using System;
using osu.Framework.Graphics;

namespace Omamori.API
{
    /// <summary>
    ///     the factory runs every time its screen is created, so return a new drawable each call
    /// </summary>
    public interface IUiHooks
    {
        /// <summary>
        ///     on top of the main menu
        /// </summary>
        void AddToMainMenu(Func<Drawable> factory);

        /// <summary>
        ///     above everything for the whole session, including during plays, created once
        /// </summary>
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
