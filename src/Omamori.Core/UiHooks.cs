using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace Omamori.Core
{
    internal sealed record UiRegistration(string ModId, Func<Drawable> Factory);

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

        // an unavailable hook is skipped
        private void register(Hook hook, Func<Drawable> factory)
        {
            if (!manifest.Hooks.ContainsKey(hook) && hinted.Add(hook))
                log.Info($"uses the {hook} hook without declaring it in {ModLoader.MANIFEST_FILENAME}, treating it as optional.");

            if (!FeatureRegistry.IsAvailable(hook))
            {
                log.Info($"{hook} hook isn't available on this osu! version, skipping this UI.");
                return;
            }

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

            foreach (var registration in list.Where(r => !ModFaults.IsFaulted(r.ModId)))
            {
                try
                {
                    var drawable = registration.Factory();
                    target.AddInternal(drawable);

                    lock (added)
                    {
                        added.RemoveAll(a => a.Drawable.IsDisposed);
                        added.Add((registration.ModId, drawable, target));
                    }
                }
                catch (Exception e)
                {
                    if (ModLoader.Get(registration.ModId) is { } mod)
                        ModFaults.Contain(mod, e, scheduler: null);
                    else
                        LoaderLog.Error($"[{registration.ModId}] UI factory threw.", e);
                }
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
