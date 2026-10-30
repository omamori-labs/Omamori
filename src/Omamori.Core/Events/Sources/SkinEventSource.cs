using Omamori.API;
using Omamori.Core.Features;
using osu.Game;

namespace Omamori.Core.Events.Sources
{
    internal static class SkinEventSource
    {
        public static void Watch(OsuGame game)
        {
            game.SkinManager.CurrentSkinInfo.BindValueChanged(e => FeatureRegistry.Run(Feature.SkinEvents, () =>
                GameEvents.RaiseChanged(GameEvent.SkinChanged, e.NewValue.PerformRead(skin => new SkinDetails { Name = skin.Name, Creator = skin.Creator ?? string.Empty }))), true);
        }
    }
}
