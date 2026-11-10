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

        [Cached]
        private readonly LazerKeyUses lazerKeys = new LazerKeyUses();

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
            lazerKeys.Forget();

            if (mod == null)
            {
                content.Add(text(InstalledModList.NONE_INSTALLED, 16));
                return;
            }

            addHeading(mod);

            // a mod declares its settings in Load, so only loaded mods have any
            var settings = mod.Id is { } id ? ModConfig.For(id)?.Declared ?? [] : [];

            // the tabs only show when there's a settings tab to switch to
            if (settings.Count > 0)
                content.Add(tabs());

            if (settings.Count > 0 && tab.Value == ModDetailsTab.Settings)
            {
                foreach (var setting in settings)
                    content.Add(setting.CreateItem());
            }
            else
            {
                addInfo(mod);
            }
        });

        private void addHeading(InstalledMod shown)
        {
            content.Add(new OsuSpriteText { Text = shown.Label, Font = OsuFont.TorusAlternate.With(size: 28) });

            if (shown.Manifest is not { } manifest)
                return;

            string[] facts = [manifest.Version, manifest.Author.Length > 0 ? $"by {manifest.Author}" : string.Empty, string.Join(", ", manifest.Capabilities)];
            content.Add(text(string.Join("  ·  ", facts.Where(f => f.Length > 0)), 14, dim: true));
        }

        private void addInfo(InstalledMod shown)
        {
            if (shown.StatusText is string status)
                content.Add(note(status, statusNoteType(shown)));

            if (shown.Manifest is not { } manifest)
                return;

            if (shown.Patches)
                content.Add(note("Changes osu!'s own code. If osu! acts up, especially after an update, try turning this off first.", SettingsNote.Type.Informational));

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
                content.Add(text($"Needs: {string.Join(", ", manifest.Dependencies.Select(describe))}", 14));

            if (manifest.Hooks.Count > 0)
                content.Add(text($"Uses: {string.Join(", ", manifest.Hooks.Select(h => describe(h.Key, h.Value)))}", 14));

            content.Add(text($"Id: {manifest.Id}", 12, dim: true));
        }

        private static SettingsNote.Type statusNoteType(InstalledMod shown)
        {
            if (shown.Pending)
                return SettingsNote.Type.Informational;

            return shown.Status == ModStatus.Crashed ? SettingsNote.Type.Critical : SettingsNote.Type.Warning;
        }

        private static string describe(ModDependency dependency)
        {
            string minVersion = dependency.MinVersion.Length > 0 ? $" {dependency.MinVersion}+" : string.Empty;
            return dependency.Id + minVersion + optionalSuffix(dependency.Optional);
        }

        private static string describe(Hook hook, HookRequirement requirement) => hookName(hook) + optionalSuffix(requirement == HookRequirement.Optional);

        private static string optionalSuffix(bool optional) => optional ? " (optional)" : string.Empty;

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

        private static SettingsNote note(string text, SettingsNote.Type type) => new SettingsNote
        {
            RelativeSizeAxes = Axes.X,
            Current = { Value = new SettingsNote.Data(text, type) },
        };

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
