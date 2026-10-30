using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;

namespace Omamori.Core
{
    internal sealed class ConfigHeader(string text) : ConfigElement
    {
        public override Drawable CreateItem() => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.GetFont(size: 20),
            Margin = new MarginPadding { Top = 8, Horizontal = 5 },
            Padding = SettingsPanel.CONTENT_PADDING,
        };
    }
}
