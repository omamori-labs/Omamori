using Omamori.API;
using Omamori.Core.Details;
using Omamori.Core.Features;
using osu.Framework.Screens;
using osu.Game;

namespace Omamori.Core.Events.Sources
{
    internal static class ScreenEventSource
    {
        public static void Watch(OsuGame game)
        {
            game.ScreenStack.ScreenPushed += changed;
            game.ScreenStack.ScreenExited += changed;
        }

        private static void changed(IScreen? last, IScreen? next) => FeatureRegistry.Run(Feature.ScreenEvents, () =>
            GameEvents.Raise(GameEvent.ScreenChanged, new ScreenChange
            {
                Screen = GameDetails.Screen(next),
                Previous = GameDetails.Screen(last),
                ScreenName = next?.GetType().Name ?? string.Empty,
            }));
    }
}
