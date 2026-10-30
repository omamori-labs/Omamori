using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    ///     a summary and the way into <see cref="ModManagerOverlay"/>, lists every mod itself only when the manager isn't available
    /// </summary>
    internal sealed partial class ModListSubsection : SettingsSubsection
    {
        private readonly List<Action> refreshes = [];

        protected override LocalisableString Header => "Mods";

        [Resolved]
        private SettingsPanel? settings { get; set; }

        // not named load, that would hide the publicized base one
        [BackgroundDependencyLoader]
        private void loadMods(GameHost host)
        {
            FeatureRegistry.Run(Feature.SettingsModList, () =>
            {
                if (!InstalledMods.Available)
                {
                    Add(note("Mod loading is off this session, so no mods are listed.", SettingsNote.Type.Critical));
                    Add(openFolderButton(host));
                    return;
                }

                var summary = new Bindable<SettingsNote.Data?>();
                Add(note(summary));
                refreshes.Add(() => summary.Value = summarise(InstalledMods.All()));

                if (ModManagerOverlay.Current is { } manager)
                {
                    Add(new SettingsButtonV2
                    {
                        Text = "Manage mods",
                        Keywords = ["omamori", "mods"],
                        Action = () => open(manager, ModFilter.All),
                    });

                    var problems = new SettingsButtonV2
                    {
                        Keywords = ["omamori", "mods", "problems"],
                        Action = () => open(manager, ModFilter.Problems),
                    };
                    Add(problems);

                    refreshes.Add(() =>
                    {
                        int count = InstalledMods.All().Count(m => m.Status is ModStatus.Problem or ModStatus.Crashed);

                        problems.Text = $"Review {count} problem(s)";
                        problems.CanBeShown.Value = count > 0;
                    });
                }

                Add(openFolderButton(host));

                if (ModManagerOverlay.Current == null)
                {
                    foreach (var mod in InstalledMods.All())
                        Add(row(mod));
                }

                refreshAll();
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ModFaults.Faulted += onFaulted;
            InstalledMods.Changed += refreshAll;
        }

        protected override void Dispose(bool isDisposing)
        {
            ModFaults.Faulted -= onFaulted;
            InstalledMods.Changed -= refreshAll;
            base.Dispose(isDisposing);
        }

        private void onFaulted(string _) => Schedule(refreshAll);

        private void open(ModManagerOverlay manager, ModFilter filter)
        {
            settings?.Hide();
            manager.Show(filter);
        }

        private void refreshAll() => refreshes.ForEach(r => r());

        private SettingsItemV2 row(InstalledMod mod)
        {
            var enabled = new BindableBool(mod.Enabled) { Disabled = mod.Manifest == null };

            var item = new SettingsItemV2(new FormCheckBox
            {
                Caption = mod.Manifest == null ? mod.Label : $"{mod.Label} {mod.Manifest.Version}",
                HintText = mod.Manifest == null ? default(LocalisableString) : ModManagerOverlay.Byline(mod.Manifest),
                Current = enabled,
            })
            {
                ShowRevertToDefaultButton = false,
                Keywords = mod.Manifest == null ? [] : [mod.Manifest.Id, mod.Manifest.Author],
            };

            enabled.BindValueChanged(e => InstalledMods.SetEnabled(mod, e.NewValue));

            refreshes.Add(() =>
            {
                if (!enabled.Disabled)
                    enabled.Value = mod.Enabled;

                item.Note.Value = mod.StatusText is string text ? new SettingsNote.Data(text, noteType(mod)) : null;
            });

            return item;
        }

        private static SettingsNote.Type noteType(InstalledMod mod) =>
            mod.Pending ? SettingsNote.Type.Informational
            : mod.Status == ModStatus.Crashed ? SettingsNote.Type.Critical
            : SettingsNote.Type.Warning;

        private static SettingsNote.Data summarise(IReadOnlyList<InstalledMod> mods)
        {
            if (mods.Count == 0)
                return new SettingsNote.Data("No mods installed. Each mod goes in its own folder inside omamori/mods.", SettingsNote.Type.Informational);

            int problems = mods.Count(m => m.Status is ModStatus.Problem or ModStatus.Crashed);
            int pending = mods.Count(m => m.Pending);

            string text = $"{mods.Count(m => m.Status == ModStatus.On)} on, {mods.Count(m => m.Status == ModStatus.Off)} off, {problems} with problems.";
            if (pending > 0)
                text += $" {pending} change(s) apply next time osu! starts.";

            return new SettingsNote.Data(text, problems > 0 ? SettingsNote.Type.Warning : SettingsNote.Type.Informational);
        }

        private static SettingsButtonV2 openFolderButton(GameHost host) => new SettingsButtonV2
        {
            Text = "Open mods folder",
            Keywords = ["omamori", "directory"],
            Action = () => ModManagerOverlay.OpenModsFolder(host),
        };

        private static Container note(string text, SettingsNote.Type type) => note(new Bindable<SettingsNote.Data?>(new SettingsNote.Data(text, type)));

        private static Container note(Bindable<SettingsNote.Data?> data) => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Padding = SettingsPanel.CONTENT_PADDING,
            Child = new SettingsNote
            {
                RelativeSizeAxes = Axes.X,
                Current = { BindTarget = data },
            },
        };
    }
}
