using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Screens.Footer;
using osu.Game.Skinning;

namespace Omamori.API
{
    /// <summary>
    ///     register from <see cref="IMod.Load"/> or the update thread, factories run on the update thread
    ///     a factory runs every time its screen is built, so return a new drawable each call, the global overlay and toolbar are built once
    /// </summary>
    public interface IModUi
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
        void AddToolbarButton(IconUsage icon, string tooltip, Action onClick, string? subTooltip = null);

        /// <summary>
        ///     an item in the right-click menu of a difficulty in song select, after osu!'s own
        ///     the details are taken when the menu opens, <paramref name="onClick"/> runs on the update thread when the item is clicked
        /// </summary>
        void AddBeatmapMenuItem(string text, Action<BeatmapDetails> onClick);

        /// <summary>an item in the right-click menu of a beatmap set in song select, see <see cref="AddBeatmapMenuItem"/></summary>
        void AddBeatmapSetMenuItem(string text, Action<BeatmapSetDetails> onClick);

        /// <summary>an item in the right-click menu of a score, on song select's leaderboard and the beatmap info overlay's</summary>
        void AddScoreMenuItem(string text, Action<ScoreDetails> onClick);

        /// <summary>
        ///     on top of the results screen after a play or when viewing a score
        /// </summary>
        void AddToResults(Func<Drawable> factory);

        /// <summary>
        ///     a component players place on the HUD themselves from the skin editor, saved in their skin like osu!'s own
        ///     it needs a public parameterless constructor, and its settings go on [SettingSource] bindables like osu!'s components
        ///     when a skin loads without this mod, its components are left out of that skin's layout and come back with the mod
        ///     saving in the skin editor meanwhile drops them for good
        /// </summary>
        void AddSkinComponent<T>() where T : Drawable, ISerialisableDrawable, new();

        /// <summary>
        ///     in the gameplay HUD, fades out with it when the player hides the HUD, gets no mouse or keyboard input
        /// </summary>
        void AddToGameplayHud(Func<Drawable> factory);
    }
}
