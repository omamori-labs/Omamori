using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Overlays;
using osu.Framework.Graphics.Containers;
using osu.Game;

namespace Omamori.Core.Events.Sources
{
    internal static class OverlayEventSource
    {
        public static void Watch(OsuGame game)
        {
            foreach (var (overlay, kind) in GameOverlays.Of(game))
            {
                overlay?.State.BindValueChanged(e => FeatureRegistry.Run(Feature.OverlayEvents, () =>
                    GameEvents.Raise(GameEvent.OverlayChanged, new OverlayChange { Overlay = kind, IsVisible =e.NewValue == Visibility.Visible })));
            }
        }
    }
}
