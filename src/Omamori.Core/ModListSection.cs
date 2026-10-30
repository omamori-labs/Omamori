using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Omamori.API;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;

namespace Omamori.Core
{
    internal sealed partial class ModListSection : SettingsSection
    {
        public override LocalisableString Header => "Omamori";

        public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.PuzzlePiece };

        public ModListSection()
        {
            Add(new ModListSubsection());
        }
    }

    /// <summary>
    ///     switches apply next launch, a mod's Load only runs once per session
    /// </summary>
    internal sealed partial class ModListSubsection : SettingsSubsection
    {
        private readonly List<Action> refreshes = [];

        protected override LocalisableString Header => "Mods";

        // not named load, that would hide the publicized base one
        [BackgroundDependencyLoader]
        private void loadMods(GameHost host)
        {
            FeatureRegistry.Run(Feature.SettingsModList, () =>
            {
                Add(new SettingsButtonV2
                {
                    Text = "Open mods folder",
                    Keywords = ["omamori", "directory"],
                    Action = () =>
                    {
                        Directory.CreateDirectory(CoreEntry.ModsDirectory);
                        host.OpenFileExternally(CoreEntry.ModsDirectory);
                    },
                });

                if (ModLoader.Settings is not { } settings)
                {
                    Add(note("Mod loading is off this session, so no mods are listed.", SettingsNote.Type.Critical));
                    return;
                }

                var mods = ModLoader.LOADED.ToList().Select(m => new Entry(label(m.Manifest), m.Manifest, true, null))
                                    .Concat(ModLoader.TURNED_OFF.Select(c => new Entry(c.Label, c.Manifest, false, null)))
                                    .Concat(ModLoader.SKIPPED.Select(s => new Entry(s.Name, s.Manifest, s.Manifest != null, s.Reason)))
                                    .OrderBy(m => m.Label, StringComparer.OrdinalIgnoreCase)
                                    .ToList();

                if (mods.Count == 0)
                    Add(note("No mods installed. Each mod goes in its own folder inside omamori/mods.", SettingsNote.Type.Informational));

                foreach (var mod in mods)
                    Add(row(settings, mod));
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ModFaults.Faulted += onFaulted;
        }

        protected override void Dispose(bool isDisposing)
        {
            ModFaults.Faulted -= onFaulted;
            base.Dispose(isDisposing);
        }

        private void onFaulted(string _) => Schedule(() => refreshes.ForEach(r => r()));

        private SettingsItemV2 row(ModSettings settings, Entry entry)
        {
            var (label, manifest, onAtLaunch, problem) = entry;
            var enabled = new BindableBool(manifest != null && settings.IsEnabled(manifest.Id)) { Disabled = manifest == null };

            var item = new SettingsItemV2(new FormCheckBox
            {
                Caption = manifest == null ? label : $"{label} {manifest.Version}",
                HintText = manifest == null ? default : hint(manifest),
                Current = enabled,
            })
            {
                ShowRevertToDefaultButton = false,
                Keywords = manifest == null ? [] : [manifest.Id, manifest.Author],
            };

            void refresh() => item.Note.Value = status(manifest, enabled.Value, onAtLaunch, problem);

            if (manifest != null)
            {
                enabled.BindValueChanged(e =>
                {
                    settings.SetEnabled(manifest.Id, e.NewValue);
                    refresh();
                });
            }

            refresh();
            refreshes.Add(refresh);
            return item;
        }

        private static SettingsNote.Data? status(ModManifest? manifest, bool enabled, bool onAtLaunch, string? problem)
        {
            if (enabled != onAtLaunch)
                return new SettingsNote.Data(enabled ? "Loads next time osu! starts." : "Turns off next time osu! starts.", SettingsNote.Type.Informational);

            if (manifest != null && ModFaults.IsFaulted(manifest.Id))
                return new SettingsNote.Data("Crashed and was turned off for this session.", SettingsNote.Type.Critical);

            return string.IsNullOrEmpty(problem) ? null : new SettingsNote.Data($"{char.ToUpperInvariant(problem[0])}{problem[1..]}.", SettingsNote.Type.Warning);
        }

        // a mod without a readable manifest can't be switched
        private sealed record Entry(string Label, ModManifest? Manifest, bool OnAtLaunch, string? Problem);

        private static string label(ModManifest manifest) => string.IsNullOrWhiteSpace(manifest.Name) ? manifest.Id : manifest.Name;

        private static LocalisableString hint(ModManifest manifest) =>
            string.Join("\n", new[] { string.IsNullOrEmpty(manifest.Author) ? null : $"by {manifest.Author}", manifest.Description }.Where(s => !string.IsNullOrEmpty(s)));

        private static Container note(string text, SettingsNote.Type type) => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Padding = SettingsPanel.CONTENT_PADDING,
            Child = new SettingsNote
            {
                RelativeSizeAxes = Axes.X,
                Current = { Value = new SettingsNote.Data(text, type) },
            },
        };
    }
}
