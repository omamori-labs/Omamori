using osu.Framework.Graphics;
using osu.Game.Overlays.Settings;

namespace Omamori.Core
{
    internal sealed class ConfigNote(string text) : ConfigElement
    {
        public override Drawable CreateItem() => SettingsLayout.Padded(new SettingsNote
        {
            RelativeSizeAxes = Axes.X,
            Current = { Value = new SettingsNote.Data(text, SettingsNote.Type.Informational) },
        });
    }
}
