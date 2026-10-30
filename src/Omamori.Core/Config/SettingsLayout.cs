using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays;

namespace Omamori.Core
{
    internal static class SettingsLayout
    {
        /// <summary>the same side padding lazer's settings items have, for anything that isn't one</summary>
        public static Container Padded(Drawable content) => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Padding = SettingsPanel.CONTENT_PADDING,
            Child = content,
        };
    }
}
