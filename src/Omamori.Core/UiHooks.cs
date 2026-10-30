using System;
using System.Collections.Generic;
using Omamori.API;
using osu.Framework.Graphics;

namespace Omamori.Core
{
    internal sealed record UiRegistration(string ModId, Func<Drawable> Factory);

    internal sealed class UiHooks : IUiHooks
    {
        public static readonly List<UiRegistration> MAIN_MENU = [];
        public static readonly List<UiRegistration> GLOBAL_OVERLAY = [];

        private readonly ModManifest manifest;
        private readonly IModLogger log;
        private readonly HashSet<Hook> hinted = [];

        public UiHooks(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            this.log = log;
        }

        public void AddToMainMenu(Func<Drawable> factory) => register(Hook.MainMenu, MAIN_MENU, factory);

        public void AddGlobalOverlay(Func<Drawable> factory) => register(Hook.GlobalOverlay, GLOBAL_OVERLAY, factory);

        // an unavailable hook is skipped
        private void register(Hook hook, List<UiRegistration> target, Func<Drawable> factory)
        {
            if (!manifest.Hooks.ContainsKey(hook) && hinted.Add(hook))
                log.Info($"uses the {hook} hook without declaring it in {ModLoader.MANIFEST_FILENAME}, treating it as optional.");

            if (!FeatureRegistry.IsAvailable(hook))
            {
                log.Info($"{hook} hook isn't available on this osu! version, skipping this UI.");
                return;
            }

            target.Add(new UiRegistration(manifest.Id, factory));
        }

        /// <summary>
        ///     creates each registered drawable and passes it to <paramref name="add"/>
        /// </summary>
        public static void Populate(IEnumerable<UiRegistration> registrations, Action<Drawable> add)
        {
            foreach (var registration in registrations)
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
