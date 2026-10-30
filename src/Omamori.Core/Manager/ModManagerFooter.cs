using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;

namespace Omamori.Core
{
    /// <summary>
    ///     what the manager puts in lazer's screen footer, the pending change count and the mods folder
    /// </summary>
    internal sealed partial class ModManagerFooter : VisibilityContainer
    {
        private OsuSpriteText pending = null!;
        private int pendingCount;

        [BackgroundDependencyLoader]
        private void loadFooter(GameHost host, OverlayColourProvider colourProvider)
        {
            Padding = new MarginPadding { Right = OsuGame.SCREEN_EDGE_MARGIN };

            InternalChild = new FillFlowContainer
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(20, 0),
                Children =
                [
                    pending = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold),
                        Colour = colourProvider.Content1,
                    },
                    new ShearedButton
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = "Open mods folder",
                        Width = 220,
                        DarkerColour = colourProvider.Colour3,
                        LighterColour = colourProvider.Colour2,
                        Action = () => ModsFolder.Open(host),
                    },
                ],
            };

            SetPending(pendingCount);
        }

        // can be called before load, the count is kept until the text exists
        public void SetPending(int count)
        {
            pendingCount = count;

            if (pending != null)
                pending.Text = count == 0 ? string.Empty : $"{count} change(s) apply next time osu! starts";
        }

        protected override void PopIn() => this.FadeIn();

        protected override void PopOut() => this.Delay(400).FadeOut();
    }
}
