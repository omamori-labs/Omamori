using System.Collections.Generic;
using Omamori.API;
using osu.Framework.Graphics.Containers;
using osu.Game;

namespace Omamori.Core.Overlays
{
    /// <summary>
    ///     the overlays <see cref="GameOverlay"/> names, shared so overlay events and navigation can't disagree about them
    /// </summary>
    internal static class GameOverlays
    {
        public static IReadOnlyList<(VisibilityContainer? Overlay, GameOverlay Kind)> Of(OsuGame game) =>
        [
            (game.Settings, GameOverlay.Settings),
            (game.Notifications, GameOverlay.Notifications),
            (game.chatOverlay, GameOverlay.Chat),
            (game.beatmapListing, GameOverlay.BeatmapListing),
            (game.beatmapSetOverlay, GameOverlay.BeatmapInfo),
            (game.dashboard, GameOverlay.Dashboard),
            (game.news, GameOverlay.News),
            (game.changelogOverlay, GameOverlay.Changelog),
            (game.wikiOverlay, GameOverlay.Wiki),
            (game.userProfile, GameOverlay.UserProfile),
        ];
    }
}
