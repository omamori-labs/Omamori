using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Screens.Footer;

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
        ///     a button in song select's footer next to mods and options, lazer lays it out and animates it like its own
        ///     only the solo song select asks for it, rooms build their own footers
        /// </summary>
        void AddToSongSelectFooter(Func<ScreenFooterButton> factory);

        /// <summary>
        ///     a button in the toolbar at the top of the screen, left of the user button, styled like osu!'s own
        ///     the toolbar is built once when the game loads, so register from <see cref="IMod.Load"/>
        /// </summary>
        void AddToolbarButton(IconUsage icon, string tooltip, Action action, string? subTooltip = null);

        /// <summary>
        ///     an item in the right-click menu of a difficulty in song select, after osu!'s own
        ///     the details are taken when the menu opens, the action runs on the update thread when the item is clicked
        /// </summary>
        void AddBeatmapMenuItem(string text, Action<BeatmapDetails> action);

        /// <summary>an item in the right-click menu of a beatmap set in song select, see <see cref="AddBeatmapMenuItem"/></summary>
        void AddBeatmapSetMenuItem(string text, Action<BeatmapSetDetails> action);

        /// <summary>an item in the right-click menu of a score, on song select's leaderboard and the beatmap info overlay's</summary>
        void AddScoreMenuItem(string text, Action<ScoreDetails> action);

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
