using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Screens.Footer;

namespace Omamori.Core.ModUi
{
    internal sealed class UiHooks : IUiHooks
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

        public void AddToResults(Func<Drawable> factory) => register(Hook.Results, factory);

        public void AddToGameplayHud(Func<Drawable> factory) => register(Hook.GameplayHud, factory);

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

        internal static void Clear()
        {
            registrations.Clear();

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
