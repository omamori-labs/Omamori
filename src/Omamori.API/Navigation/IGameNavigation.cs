namespace Omamori.API
{
    /// <summary>
    ///     moving around the game the way the player would, calls can come from any thread and are carried out on the update thread
    ///     nothing happens before the game has finished loading, or when <see cref="Hook.Navigation"/> isn't available
    /// </summary>
    public interface IGameNavigation
    {
        /// <summary>
        ///     goes to song select with the difficulty selected, leaving the screen the player is on like the beatmap listing's buttons do
        ///     false when it isn't in the library, <paramref name="onlineId"/> is <see cref="BeatmapDetails.OnlineId"/>
        /// </summary>
        bool SelectBeatmap(int onlineId);

        /// <summary>
        ///     <see cref="SelectBeatmap"/> then starts a solo play of it with the mods already selected
        ///     it goes through <see cref="IGamePlays.OnStarting"/> like any other start, so another mod can still cancel it
        /// </summary>
        bool PlayBeatmap(int onlineId);

        /// <summary>false for <see cref="GameOverlay.Other"/>, or an overlay this osu! version doesn't have</summary>
        bool ShowOverlay(GameOverlay overlay);

        void HideOverlays();

        /// <summary>a difficulty's page in the beatmap info overlay, it doesn't need to be in the library</summary>
        void ShowBeatmapPage(int onlineId);

        void SearchBeatmaps(string query);

        void ShowUser(int userId);

        /// <summary>a path under osu.ppy.sh/wiki, like "Game_mode/osu!"</summary>
        void ShowWiki(string path);

        /// <summary>osu! links open in game, anything else in the browser after osu!'s usual warning</summary>
        void OpenLink(string url);
    }
}
