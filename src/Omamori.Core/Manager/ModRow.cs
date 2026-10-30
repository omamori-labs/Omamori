using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;

namespace Omamori.Core.Manager
{
    /// <summary>
    ///     one mod in the manager's list, a coloured status bar, its name and status, and a quick switch
    /// </summary>
    internal sealed partial class ModRow : OsuClickableContainer
    {
        public readonly InstalledMod Mod;

        private readonly Bindable<InstalledMod?> selected;
        private readonly BindableBool enabled;

        private Box background = null!;
        private Box bar = null!;
        private OsuSpriteText status = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public ModRow(InstalledMod mod, Bindable<InstalledMod?> selected)
        {
            Mod = mod;
            this.selected = selected;
            enabled = new BindableBool(mod.Enabled) { Disabled = mod.Manifest == null };

            RelativeSizeAxes = Axes.X;
            Height = 50;
            Masking = true;
            CornerRadius = 7;
            Action = () => selected.Value = Mod;
        }

        [BackgroundDependencyLoader]
        private void loadRow()
        {
            Children =
            [
                background = new Box { RelativeSizeAxes = Axes.Both },
                bar = new Box { RelativeSizeAxes = Axes.Y, Width = 5 },
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Margin = new MarginPadding { Left = 18 },
                    Children =
                    [
                        new OsuSpriteText
                        {
                            Text = Mod.Manifest == null ? Mod.Label : $"{Mod.Label}  {Mod.Manifest.Version}",
                            Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold),
                        },
                        status = new OsuSpriteText { Font = OsuFont.GetFont(size: 12) },
                    ],
                },
                new SwitchButton
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 12 },
                    Current = enabled,
                },
            ];

            enabled.BindValueChanged(e => InstalledModList.SetEnabled(Mod, e.NewValue));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            selected.BindValueChanged(_ => updateBackground(), true);
            Refresh();
        }

        public void Refresh()
        {
            if (!enabled.Disabled)
                enabled.Value = Mod.Enabled;

            if (!IsLoaded)
                return;

            (string text, var colour) = Mod.Status switch
            {
                ModStatus.Crashed => ("crashed this session", colours.Red1),
                ModStatus.Problem => ("didn't load", colours.Orange1),
                ModStatus.Off => ("off", colourProvider.Foreground1),
                _ => ("on", colours.Green1),
            };

            if (Mod.Pending)
                text += Mod.Enabled ? ", loads next launch" : ", turns off next launch";

            status.Text = text;
            status.Colour = colour;
            bar.Colour = colour;
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateBackground();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateBackground();
            base.OnHoverLost(e);
        }

        private void updateBackground() =>
            background.Colour = selected.Value == Mod ? colourProvider.Background2 : IsHovered ? colourProvider.Background3 : colourProvider.Background4;
    }
}
