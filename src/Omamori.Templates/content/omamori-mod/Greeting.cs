using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace OmamoriMod
{
    /// <summary>
    ///     partial so osu's source generator writes its dependency injection
    /// </summary>
    public partial class Greeting : CompositeDrawable
    {
        private readonly string modName;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public Greeting(string modName)
        {
            this.modName = modName;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;
            Position = new Vector2(20, 60);

            InternalChild = new OsuSpriteText
            {
                Text = $"hello from {modName}",
                Font = OsuFont.GetFont(size: 24, weight: FontWeight.Bold),
                Colour = colours.Pink,
            };
        }
    }
}
