using System;
using System.Collections.Generic;
using Omamori.API;
using osu.Framework.Graphics;

namespace Omamori.Core
{
    internal record UiRegistration(string ModId, Func<Drawable> Factory);

    internal class UiHooks : IUiHooks
    {
        public static readonly List<UiRegistration> MAIN_MENU = [];
        public static readonly List<UiRegistration> GLOBAL_OVERLAY = [];

        private readonly string modId;

        public UiHooks(string modId)
        {
            this.modId = modId;
        }

        public void AddToMainMenu(Func<Drawable> factory) => MAIN_MENU.Add(new UiRegistration(modId, factory));

        public void AddGlobalOverlay(Func<Drawable> factory) => GLOBAL_OVERLAY.Add(new UiRegistration(modId, factory));

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
