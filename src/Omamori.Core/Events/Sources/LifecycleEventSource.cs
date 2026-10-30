using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Patching;
using osu.Framework.Configuration;
using osu.Game;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     the window, and the game closing, see <see cref="Patches.GameExitingHook"/>
    /// </summary>
    internal static class LifecycleEventSource
    {
        private static bool exiting;

        public static void Watch(OsuGame game)
        {
            void raise() => FeatureRegistry.Run(Feature.LifecycleEvents, () => GameEvents.RaiseChanged(GameEvent.WindowChanged, new WindowDetails
            {
                Focused = game.Host.IsActive.Value,
                Fullscreen = game.Host.Window is { } window && window.WindowMode.Value != WindowMode.Windowed,
            }));

            // both can change off the update thread
            game.Host.IsActive.BindValueChanged(_ => game.Schedule(raise));
            game.Host.Window?.WindowMode.BindValueChanged(_ => game.Schedule(raise));
            raise();
        }

        // exit can be asked for more than once
        public static void Exiting()
        {
            if (exiting)
                return;

            exiting = true;
            GameEvents.Raise(GameEvent.GameExiting, true);
        }
    }
}
