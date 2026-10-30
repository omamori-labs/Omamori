using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;

namespace Omamori.Core
{
    internal sealed partial class OmamoriSettingsSection : SettingsSection
    {
        public override LocalisableString Header => "Omamori";

        public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.PuzzlePiece };

        public OmamoriSettingsSection()
        {
            Add(new ModsSettingsSubsection());
        }
    }
}
