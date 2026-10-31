using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace Omamori.Core.ModUi
{
    internal sealed class UiHooks : IUiHooks
    {
        private static readonly Dictionary<Hook, List<UiRegistration>> registrations = [];

        private static readonly List<(string ModId, Drawable Drawable, CompositeDrawable Target)> added = [];

        private readonly ModManifest manifest;
        private readonly IModLogger log;
        private readonly HashSet<Hook> hinted = [];

        public UiHooks(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            this.log = log;
        }

        public void AddToMainMenu(Func<Drawable> factory) => register(Hook.MainMenu, factory);

        public void AddGlobalOverlay(Func<Drawable> factory) => register(Hook.GlobalOverlay, factory);

        public void AddToSongSelect(Func<Drawable> factory) => register(Hook.SongSelect, factory);

        public void AddToResults(Func<Drawable> factory) => register(Hook.Results, factory);

        public void AddToGameplayHud(Func<Drawable> factory) => register(Hook.GameplayHud, factory);

        private void register(Hook hook, Func<Drawable> factory)
        {
            if (!HookCheck.CanRegister(manifest, log, hinted, hook, "UI"))
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
