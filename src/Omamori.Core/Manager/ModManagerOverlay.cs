using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Game;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;
using osuTK;

namespace Omamori.Core
{
    /// <summary>
    ///     full screen list of every installed mod, opened from settings, built like mod select and first-run setup
    /// </summary>
    internal sealed partial class ModManagerOverlay : ShearedOverlayContainer
    {
        public static ModManagerOverlay? Current { get; set; }

        public static bool Available => Current != null && FeatureRegistry.IsActive(Feature.ModManager);

        private readonly Bindable<string> search = new Bindable<string>(string.Empty);
        private readonly Bindable<ModFilter> filter = new Bindable<ModFilter>();
        private readonly Bindable<InstalledMod?> selected = new Bindable<InstalledMod?>();

        private readonly Dictionary<ModFilter, ShearedToggleButton> filterButtons = [];

        private FillFlowContainer<ModRow> list = null!;
        private ModDetailsPane details = null!;
        private ShearedSearchTextBox searchBox = null!;
        private ModManagerFooter? footerContent;

        public ModManagerOverlay()
            : base(OverlayColourScheme.Pink)
        {
        }

        [BackgroundDependencyLoader]
        private void loadManager()
        {
            Header.Title = "Omamori";
            Header.Description = "Manage mods here.";

            var toolbar = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(PADDING, 0),
                Child = searchBox = new ShearedSearchTextBox
                {
                    HoldFocus = false,
                    Width = 300,
                    Current = search,
                },
            };

            foreach (var value in Enum.GetValues<ModFilter>())
            {
                var button = filterButtons[value] = new ShearedToggleButton { Width = 150 };
                button.Active.BindValueChanged(e =>
                {
                    if (e.NewValue)
                        filter.Value = value;
                    else if (filter.Value == value)
                        button.Active.Value = true;
                });
                toolbar.Add(button);
            }

            // the colour picker and file selector in mod settings open as popovers
            MainAreaContent.Add(new PopoverContainer
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Horizontal = OsuGame.SCREEN_EDGE_MARGIN, Top = PADDING },
                Child = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = [new Dimension(GridSizeMode.AutoSize), new Dimension(GridSizeMode.Absolute, PADDING), new Dimension()],
                    Content = new[]
                    {
                        new Drawable[] { toolbar },
                        [Empty()],
                        [
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                ColumnDimensions = [new Dimension(GridSizeMode.Relative, 0.45f, maxSize: 640), new Dimension(GridSizeMode.Absolute, PADDING), new Dimension()],
                                Content = new[]
                                {
                                    new[]
                                    {
                                        panel(list = new FillFlowContainer<ModRow>
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 4),
                                        }),
                                        Empty(),
                                        panel(details = new ModDetailsPane()),
                                    },
                                },
                            },
                        ],
                    },
                },
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            filter.BindValueChanged(e =>
            {
                foreach (var (value, button) in filterButtons)
                    button.Active.Value = value == e.NewValue;

                applyFilter();
            }, true);

            search.BindValueChanged(_ => applyFilter());
            selected.BindValueChanged(e => details.Display(e.NewValue));

            InstalledModList.Changed += scheduleRefresh;
            ModConfig.PendingChanged += updatePending;
            ModFaults.Faulted += onFaulted;
            FeatureRegistry.Faulted += onFeatureFaulted;
        }

        protected override void Dispose(bool isDisposing)
        {
            InstalledModList.Changed -= scheduleRefresh;
            ModConfig.PendingChanged -= updatePending;
            ModFaults.Faulted -= onFaulted;
            FeatureRegistry.Faulted -= onFeatureFaulted;
            base.Dispose(isDisposing);
        }

        public void Show(ModFilter initial)
        {
            filter.Value = initial;
            Show();
        }

        public override VisibilityContainer CreateFooterContent() => footerContent = new ModManagerFooter { RelativeSizeAxes = Axes.Both };

        protected override void PopIn()
        {
            // the footer only takes one overlay at a time, it throws otherwise
            if (footer?.ActiveOverlay is { } other && other != this)
                other.Hide();

            base.PopIn();
            guard(rebuild);
            searchBox.TakeFocus();
        }

        private void onFaulted(string _) => scheduleRefresh();

        private void onFeatureFaulted(Feature feature)
        {
            if (feature == Feature.ModManager)
                Schedule(Hide);
        }

        // never from inside a switch's own callback, refresh rebuilds the details pane
        private void scheduleRefresh() => Schedule(refresh);

        // the mod list only changes per launch, so rebuilding on open is enough
        private void rebuild()
        {
            var mods = InstalledModList.All();
            string? keep = selected.Value?.Label;

            list.ChildrenEnumerable = mods.Select(m => new ModRow(m, selected));
            selected.Value = mods.FirstOrDefault(m => m.Label == keep) ?? (mods.Count > 0 ? mods[0] : null);

            refresh();
        }

        private void refresh() => guard(() =>
        {
            var mods = list.Children.Select(i => i.Mod).ToList();

            foreach (var row in list.Children)
                row.Refresh();

            foreach (var (value, button) in filterButtons)
                button.Text = $"{value} ({mods.Count(m => matches(m, value))})";

            updatePending();
            applyFilter();
            details.Display(selected.Value);
        });

        // only the footer, a settings change must not rebuild the pane under the slider being dragged
        private void updatePending() => footerContent?.SetPending(list.Children.Count(i => i.Mod.Pending) + ModConfig.PendingCount());

        private void applyFilter()
        {
            string[] terms = search.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var row in list.Children)
            {
                string haystack = string.Join(' ', row.Mod.Label, row.Mod.Manifest?.Id, row.Mod.Manifest?.Author, row.Mod.Manifest?.Description);
                bool visible = matches(row.Mod, filter.Value) && terms.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));

                row.Alpha = visible ? 1 : 0;
            }
        }

        private static bool matches(InstalledMod mod, ModFilter filter) => filter switch
        {
            ModFilter.On => mod.Status == ModStatus.On,
            ModFilter.Off => mod.Status == ModStatus.Off,
            ModFilter.Problems => mod.Status is ModStatus.Problem or ModStatus.Crashed,
            _ => true,
        };

        private InputBlockingContainer panel(Drawable content) => new InputBlockingContainer
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            CornerRadius = 10,
            Children =
            [
                new Box { RelativeSizeAxes = Axes.Both, Colour = ColourProvider.Background5 },
                new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarOverlapsContent = false,
                    Padding = new MarginPadding(PADDING),
                    Child = content,
                },
            ],
        };

        // ui bugs turn the manager off instead of counting towards osu!'s crash limit
        private static void guard(Action action) => FeatureRegistry.Run(Feature.ModManager, action);
    }
}
