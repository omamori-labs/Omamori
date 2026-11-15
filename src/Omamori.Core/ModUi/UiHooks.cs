using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Skinning;
using osu.Game.Screens.Footer;

namespace Omamori.Core.ModUi
{
    internal sealed class UiHooks : IModUi
    {
        private static readonly Dictionary<Hook, List<UiRegistration>> registrations = [];

        private static readonly List<(string ModId, Drawable Drawable, CompositeDrawable Target)> added = [];

        private readonly ModManifest manifest;
        private readonly HookCheck hooks;

        public UiHooks(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            hooks = new HookCheck(manifest, log);
        }

        public void AddToMainMenu(Func<Drawable> factory) => register(Hook.MainMenu, factory);

        public void AddGlobalOverlay(Func<Drawable> factory) => register(Hook.GlobalOverlay, factory);

        public void AddToSongSelect(Func<Drawable> factory) => register(Hook.SongSelect, factory);

        public void AddToSongSelectFooter(Func<ScreenFooterButton> factory) => register(Hook.SongSelectFooter, factory);

        public void AddToolbarButton(IconUsage icon, string tooltip, Action action, string? subTooltip = null)
        {
            string modId = manifest.Id;
            register(Hook.Toolbar, () => new ModToolbarButton(icon, tooltip, subTooltip, () => ModFaults.RunForMod(modId, "toolbar button", action)));
        }

        public void AddBeatmapMenuItem(string text, Action<BeatmapDetails> action) => addMenuItem(MenuTarget.Beatmap, text, d => action((BeatmapDetails)d));

        public void AddBeatmapSetMenuItem(string text, Action<BeatmapSetDetails> action) => addMenuItem(MenuTarget.BeatmapSet, text, d => action((BeatmapSetDetails)d));

        public void AddScoreMenuItem(string text, Action<ScoreDetails> action) => addMenuItem(MenuTarget.Score, text, d => action((ScoreDetails)d));

        public void AddSkinComponent<T>() where T : Drawable, ISerialisableDrawable, new()
        {
            if (hooks.CanRegister(Hook.SkinComponents, "skin component"))
                SkinComponents.Add(manifest.Id, typeof(T));
        }

        public void AddToResults(Func<Drawable> factory) => register(Hook.Results, factory);

        public void AddToGameplayHud(Func<Drawable> factory) => register(Hook.GameplayHud, factory);

        private void addMenuItem(MenuTarget target, string text, Action<object> action)
        {
            if (hooks.CanRegister(Hook.ContextMenus, "menu item"))
                ContextMenus.Add(manifest.Id, target, text, action);
        }

        private void register(Hook hook, Func<Drawable> factory)
        {
            if (!hooks.CanRegister(hook, "UI"))
                return;

            if (!registrations.TryGetValue(hook, out var list))
                registrations[hook] = list = [];

            list.Add(new UiRegistration(manifest.Id, factory));
        }

        /// <summary>
        ///     a factory that throws turns its mod off
        /// </summary>
        public static void Populate(Hook hook, CompositeDrawable target)
        {
            if (!registrations.TryGetValue(hook, out var list))
                return;

            foreach (var registration in list)
            {
                ModFaults.RunForMod(registration.ModId, "UI factory", () =>
                {
                    var drawable = registration.Factory();
                    target.AddInternal(drawable);

                    lock (added)
                    {
                        added.RemoveAll(a => a.Drawable.IsDisposed);
                        added.Add((registration.ModId, drawable, target));
                    }
                });
            }
        }

        /// <summary>
        ///     a factory that throws turns its mod off, a button that isn't removed with it is gone when the footer is next built
        /// </summary>
        public static IReadOnlyList<ScreenFooterButton> FooterButtons()
        {
            var buttons = new List<ScreenFooterButton>();

            if (!registrations.TryGetValue(Hook.SongSelectFooter, out var list))
                return buttons;

            foreach (var registration in list)
            {
                ModFaults.RunForMod(registration.ModId, "UI factory", () =>
                {
                    if (registration.Factory() is ScreenFooterButton button)
                        buttons.Add(button);
                });
            }

            return buttons;
        }

        /// <summary>
        ///     into the toolbar's right-hand row just before the user button, in mod load order
        ///     added to the same list as other mod ui, so a mod that faults loses its buttons too
        /// </summary>
        public static void PopulateToolbar(FillFlowContainer row, Drawable userButton)
        {
            if (!registrations.TryGetValue(Hook.Toolbar, out var list) || list.Count == 0)
                return;

            // lazer's buttons all sit at layout position 0 and keep the order they were added in, so they get real positions first
            var ordered = row.Children.Select((child, index) => (child, index))
                             .OrderBy(c => row.GetLayoutPosition(c.child))
                             .ThenBy(c => c.index)
                             .Select(c => c.child)
                             .ToList();

            for (int i = 0; i < ordered.Count; i++)
                row.SetLayoutPosition(ordered[i], i);

            float before = ordered.IndexOf(userButton);
            float step = 1f / (list.Count + 1);

            for (int i = 0; i < list.Count; i++)
            {
                var registration = list[i];
                float position = before - 1 + (step * (i + 1));

                ModFaults.RunForMod(registration.ModId, "UI factory", () =>
                {
                    var button = registration.Factory();
                    row.Add(button);
                    row.SetLayoutPosition(button, position);

                    lock (added)
                        added.Add((registration.ModId, button, row));
                });
            }
        }

        internal static void Clear()
        {
            registrations.Clear();
            ContextMenus.Clear();
            SkinComponents.Clear();

            lock (added)
                added.Clear();
        }

        // update thread only
        public static int Remove(string modId)
        {
            List<(string ModId, Drawable Drawable, CompositeDrawable Target)> ours;

            lock (added)
            {
                ours = [.. added.Where(a => a.ModId == modId)];
                added.RemoveAll(a => a.ModId == modId);
            }

            // skin components aren't in the list, the skin is built again without them instead
            SkinComponents.ModTurnedOff(modId);

            int removed = 0;

            foreach (var (_, drawable, target) in ours)
            {
                try
                {
                    if (target.RemoveInternal(drawable, disposeImmediately: true))
                        removed++;
                }
                catch (Exception e)
                {
                    LoaderLog.Error($"[{modId}] couldn't remove {drawable.GetType().Name}.", e);
                }
            }

            return removed;
        }
    }
}
