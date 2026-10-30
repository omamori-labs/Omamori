using Omamori.API;
using osu.Framework.Graphics.Containers;
using osu.Game;

namespace Omamori.Core
{
    internal static class OverlayEventSource
    {
        public static void Watch(OsuGame game)
        {
            (VisibilityContainer? Overlay, GameOverlay Kind)[] overlays =
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

            foreach (var (overlay, kind) in overlays)
            {
                overlay?.State.BindValueChanged(e => FeatureRegistry.Run(Feature.OverlayEvents, () =>
                    GameEvents.Raise(GameEvent.OverlayChanged, new OverlayChange { Overlay = kind, Visible = e.NewValue == Visibility.Visible })));
            }
        }
    }
}
