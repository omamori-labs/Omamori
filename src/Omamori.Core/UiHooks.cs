using System;
using System.Collections.Generic;
using Omamori.API;
using osu.Framework.Graphics;

namespace Omamori.Core
{
    internal sealed record UiRegistration(string ModId, Func<Drawable> Factory);

    internal sealed class UiHooks : IUiHooks
    {
        private static readonly Dictionary<Hook, List<UiRegistration>> registrations = [];

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
        ///     creates each drawable registered for <paramref name="hook"/> and passes it to <paramref name="add"/>
        /// </summary>
        public static void Populate(Hook hook, Action<Drawable> add)
        {
            if (!registrations.TryGetValue(hook, out var list))
                return;

            foreach (var registration in list)
            {
                try
                {
                    add(registration.Factory());
                }
                catch (Exception e)
                {
                    LoaderLog.Error($"[{registration.ModId}] UI factory threw.", e);
                }
            }
        }
    }
}
