using osu.Framework.Allocation;
using osu.Framework.Bindables;
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
        private readonly IBindable<string> text;
        private readonly IBindable<bool> shown;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public Greeting(IBindable<string> text, IBindable<bool> shown)
        {
            this.text = text.GetBoundCopy();
            this.shown = shown.GetBoundCopy();
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;
            Position = new Vector2(20, 60);

            var label = new OsuSpriteText
            {
                Font = OsuFont.GetFont(size: 24, weight: FontWeight.Bold),
                Colour = colours.Pink,
            };

            InternalChild = label;
            text.BindValueChanged(e => label.Text = e.NewValue, true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            shown.BindValueChanged(e => this.FadeTo(e.NewValue ? 1 : 0, 200, Easing.OutQuint), true);
        }
    }
}
