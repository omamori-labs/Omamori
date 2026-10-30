using System;
using System.Linq;
using Omamori.API;
using Omamori.Core.Config;
using Omamori.Core.Features;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;
using osu.Game.Overlays.Settings;
using osuTK;

namespace Omamori.Core.Manager
{
    /// <summary>
    ///     the right side of the mod manager, an info tab and, for mods that declared any, a settings tab
    /// </summary>
    internal sealed partial class ModDetailsPane : CompositeDrawable
    {
        private readonly Bindable<ModDetailsTab> tab = new Bindable<ModDetailsTab>();
        private readonly FillFlowContainer content;
        private InstalledMod? mod;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public ModDetailsPane()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChild = content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            tab.BindValueChanged(_ => rebuild());
        }

        public void Display(InstalledMod? shown)
        {
            mod = shown;
            rebuild();
        }

        private void rebuild() => FeatureRegistry.Run(Feature.ModManager, () =>
        {
            content.Clear();

            if (mod == null)
            {
                content.Add(text("No mods installed. Each mod goes in its own folder inside omamori/mods.", 16));
                return;
            }

            content.Add(new OsuSpriteText { Text = mod.Label, Font = OsuFont.TorusAlternate.With(size: 28) });

            if (mod.Manifest is { } manifest)
            {
                string[] facts = [manifest.Version, manifest.Author.Length > 0 ? $"by {manifest.Author}" : string.Empty, string.Join(", ", manifest.Capabilities)];
                content.Add(text(string.Join("  ·  ", facts.Where(f => f.Length > 0)), 14, dim: true));
            }

            // a mod declares its settings in Load, so only loaded mods have any
            var settings = mod.Id is { } id ? ModConfig.For(id)?.Declared ?? [] : [];

            if (settings.Count == 0)
            {
                addInfo(mod);
                return;
            }

            content.Add(tabs());

            if (tab.Value == ModDetailsTab.Settings)
            {
                foreach (var setting in settings)
                    content.Add(setting.CreateItem());
            }
            else
            {
                addInfo(mod);
            }
        });

        private void addInfo(InstalledMod shown)
        {
            if (shown.StatusText is string status)
            {
                content.Add(new SettingsNote
                {
                    RelativeSizeAxes = Axes.X,
                    Current = { Value = new SettingsNote.Data(status, shown.Pending ? SettingsNote.Type.Informational : shown.Status == ModStatus.Crashed ? SettingsNote.Type.Critical : SettingsNote.Type.Warning) },
                });
            }

            if (shown.Manifest is not { } manifest)
                return;

            var enabled = new BindableBool(shown.Enabled);
            enabled.BindValueChanged(e => InstalledModList.SetEnabled(shown, e.NewValue));
            content.Add(new FormCheckBox { Caption = "Load this mod", Current = enabled });

            if (shown.CanRetry)
            {
                content.Add(new ShearedButton
                {
                    Text = "Try again next launch",
                    Width = 260,
                    Action = () => InstalledModList.Retry(shown),
                });
            }

            if (manifest.Description.Length > 0)
                content.Add(text(manifest.Description, 16));

            if (manifest.Dependencies.Count > 0)
                content.Add(text($"Needs: {string.Join(", ", manifest.Dependencies.Select(d => d.Id + (d.MinVersion.Length > 0 ? $" {d.MinVersion}+" : string.Empty) + (d.Optional ? " (optional)" : string.Empty)))}", 14));

            if (manifest.Hooks.Count > 0)
                content.Add(text($"Uses: {string.Join(", ", manifest.Hooks.Select(h => hookName(h.Key) + (h.Value == HookRequirement.Optional ? " (optional)" : string.Empty)))}", 14));

            content.Add(text($"Id: {manifest.Id}", 12, dim: true));
        }

        private FillFlowContainer tabs()
        {
            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(ShearedOverlayContainer.PADDING, 0),
                Margin = new MarginPadding { Vertical = 4 },
            };

            foreach (var value in Enum.GetValues<ModDetailsTab>())
            {
                var button = new ShearedToggleButton { Text = value.ToString(), Width = 140, Height = 36, Active = { Value = tab.Value == value } };
                button.Active.BindValueChanged(e =>
                {
                    // switching rebuilds the pane this button is in
                    if (e.NewValue)
                        Schedule(() => tab.Value = value);
                    else if (tab.Value == value)
                        button.Active.Value = true;
                });
                flow.Add(button);
            }

            return flow;
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
                s.Colour = colourProvider.Content2;
        })
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Text = content,
        };
    }
}
