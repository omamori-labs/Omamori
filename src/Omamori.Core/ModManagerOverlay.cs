using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Omamori.API;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;
using osu.Game.Overlays.Settings;
using osuTK;

namespace Omamori.Core
{
    internal enum ModFilter
    {
        All,
        On,
        Off,
        Problems,
    }

    /// <summary>
    ///     full screen list of every installed mod, opened from settings, built like mod select and first-run setup
    /// </summary>
    internal sealed partial class ModManagerOverlay : ShearedOverlayContainer
    {
        public static ModManagerOverlay? Current { get; set; }

        private readonly Bindable<string> search = new Bindable<string>(string.Empty);
        private readonly Bindable<ModFilter> filter = new Bindable<ModFilter>();
        private readonly Bindable<InstalledMod?> selected = new Bindable<InstalledMod?>();

        private readonly Dictionary<ModFilter, ShearedToggleButton> filterButtons = [];

        private FillFlowContainer<ModListItem> list = null!;
        private FillFlowContainer details = null!;
        private ShearedSearchTextBox searchBox = null!;
        private ManagerFooterContent? footerContent;

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

            MainAreaContent.Add(new Container
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
                                        panel(list = new FillFlowContainer<ModListItem>
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 4),
                                        }),
                                        Empty(),
                                        panel(details = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 10),
                                        }),
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
            selected.BindValueChanged(_ => guard(showDetails));

            InstalledMods.Changed += scheduleRefresh;
            ModFaults.Faulted += onFaulted;
        }

        protected override void Dispose(bool isDisposing)
        {
            InstalledMods.Changed -= scheduleRefresh;
            ModFaults.Faulted -= onFaulted;
            base.Dispose(isDisposing);
        }

        public void Show(ModFilter initial)
        {
            filter.Value = initial;
            Show();
        }

        public override VisibilityContainer CreateFooterContent() => footerContent = new ManagerFooterContent();

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

        // never from inside a switch's own callback, refresh rebuilds the details pane
        private void scheduleRefresh() => Schedule(refresh);

        // the mod list only changes per launch, so rebuilding on open is enough
        private void rebuild()
        {
            var mods = InstalledMods.All();
            string? keep = selected.Value?.Label;

            list.ChildrenEnumerable = mods.Select(m => new ModListItem(m, selected));
            selected.Value = mods.FirstOrDefault(m => m.Label == keep) ?? (mods.Count > 0 ? mods[0] : null);

            refresh();
        }

        private void refresh() => guard(() =>
        {
            var mods = list.Children.Select(i => i.Mod).ToList();

            foreach (var item in list.Children)
                item.Refresh();

            foreach (var (value, button) in filterButtons)
                button.Text = $"{value} ({mods.Count(m => matches(m, value))})";

            footerContent?.SetPending(mods.Count(m => m.Pending));
            applyFilter();
            showDetails();
        });

        private void applyFilter()
        {
            string[] terms = search.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in list.Children)
            {
                string haystack = string.Join(' ', item.Mod.Label, item.Mod.Manifest?.Id, item.Mod.Manifest?.Author, item.Mod.Manifest?.Description);
                bool visible = matches(item.Mod, filter.Value) && terms.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));

                item.Alpha = visible ? 1 : 0;
            }
        }

        private static bool matches(InstalledMod mod, ModFilter filter) => filter switch
        {
            ModFilter.On => mod.Status == ModStatus.On,
            ModFilter.Off => mod.Status == ModStatus.Off,
            ModFilter.Problems => mod.Status is ModStatus.Problem or ModStatus.Crashed,
            _ => true,
        };

        private void showDetails()
        {
            details.Clear();

            if (selected.Value is not { } mod)
            {
                details.Add(text("No mods installed. Each mod goes in its own folder inside omamori/mods.", 16));
                return;
            }

            details.Add(new OsuSpriteText { Text = mod.Label, Font = OsuFont.TorusAlternate.With(size: 28) });

            if (mod.Manifest is { } manifest)
            {
                string[] facts = [manifest.Version, manifest.Author.Length > 0 ? $"by {manifest.Author}" : string.Empty, string.Join(", ", manifest.Capabilities)];
                details.Add(text(string.Join("  ·  ", facts.Where(f => f.Length > 0)), 14, dim: true));
            }

            if (mod.StatusText is string status)
            {
                details.Add(new SettingsNote
                {
                    RelativeSizeAxes = Axes.X,
                    Current = { Value = new SettingsNote.Data(status, mod.Pending ? SettingsNote.Type.Informational : mod.Status == ModStatus.Crashed ? SettingsNote.Type.Critical : SettingsNote.Type.Warning) },
                });
            }

            if (mod.Manifest is not { } m)
                return;

            var enabled = new BindableBool(mod.Enabled);
            enabled.BindValueChanged(e => InstalledMods.SetEnabled(mod, e.NewValue));
            details.Add(new FormCheckBox { Caption = "Load this mod", Current = enabled });

            if (mod.CanRetry)
            {
                details.Add(new ShearedButton
                {
                    Text = "Try again next launch",
                    Width = 260,
                    Action = () => InstalledMods.Retry(mod),
                });
            }

            if (m.Description.Length > 0)
                details.Add(text(m.Description, 16));

            if (m.Dependencies.Count > 0)
                details.Add(text($"Needs: {string.Join(", ", m.Dependencies.Select(d => d.Id + (d.MinVersion.Length > 0 ? $" {d.MinVersion}+" : string.Empty) + (d.Optional ? " (optional)" : string.Empty)))}", 14));

            if (m.Hooks.Count > 0)
                details.Add(text($"Uses: {string.Join(", ", m.Hooks.Select(h => hookName(h.Key) + (h.Value == HookRequirement.Optional ? " (optional)" : string.Empty)))}", 14));

            details.Add(text($"Id: {m.Id}", 12, dim: true));
        }

        private static string hookName(Hook hook)
        {
            try
            {
                return FeatureRegistry.Info(FeatureRegistry.ForHook(hook)).Name;
            }
            catch (ArgumentOutOfRangeException)
            {
                return hook.ToString();
            }
        }

        private OsuTextFlowContainer text(string content, float size, bool dim = false) => new OsuTextFlowContainer(s =>
        {
            s.Font = OsuFont.GetFont(size: size);
            if (dim)
                s.Colour = ColourProvider.Content2;
        })
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Text = content,
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

        public static void OpenModsFolder(GameHost host)
        {
            Directory.CreateDirectory(CoreEntry.ModsDirectory);
            host.OpenFileExternally(CoreEntry.ModsDirectory);
        }

        private sealed partial class ModListItem : OsuClickableContainer
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

            public ModListItem(InstalledMod mod, Bindable<InstalledMod?> selected)
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
            private void loadItem()
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

                enabled.BindValueChanged(e => InstalledMods.SetEnabled(Mod, e.NewValue));
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

        private sealed partial class ManagerFooterContent : VisibilityContainer
        {
            private OsuSpriteText pending = null!;
            private int pendingCount;

            [BackgroundDependencyLoader]
            private void loadFooter(GameHost host, OverlayColourProvider colourProvider)
            {
                RelativeSizeAxes = Axes.Both;
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
                            Action = () => OpenModsFolder(host),
                        },
                    ],
                };

                SetPending(pendingCount);
            }

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
}
