using System;
using System.Collections.Generic;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Framework.Bindables;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.States;

namespace Omamori.Core.Input
{
    internal sealed class ModInput : IModInput
    {
        private static readonly List<Registration> registrations = [];

        private readonly ModManifest manifest;
        private readonly HookCheck hooks;

        public ModInput(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            hooks = new HookCheck(manifest, log);
        }

        public IDisposable OnPressed(IBindable<ModKeys> keys, Action handler) => register(keys, handler, onRelease: false);

        public IDisposable OnReleased(IBindable<ModKeys> keys, Action handler) => register(keys, handler, onRelease: true);

        private IDisposable register(IBindable<ModKeys> keys, Action handler, bool onRelease)
        {
            if (!hooks.CanRegister(Hook.Input, "key binding"))
                return Subscription.NONE;

            var registration = new Registration(manifest.Id, keys.GetBoundCopy(), handler, onRelease);

            lock (registrations)
                registrations.Add(registration);

            return new Subscription(() =>
            {
                lock (registrations)
                    registrations.Remove(registration);
            });
        }

        /// <summary>
        ///     update thread only, modifiers match exactly so ctrl+k doesn't also fire k, other held keys and buttons don't matter
        /// </summary>
        public static void KeysChanged(KeyCombination pressed, InputState state)
        {
            List<Registration> all;

            lock (registrations)
                all = [.. registrations];

            foreach (var registration in all)
            {
                var binding = registration.Keys.Value;
                bool held = binding is { IsNone: false } && binding.ToCombination().IsPressed(pressed, state, KeyCombinationMatchingMode.Modifiers);

                if (held == registration.Held)
                    continue;

                registration.Held = held;

                if (held != registration.OnRelease)
                    ModFaults.RunForMod(registration.ModId, "key binding handler", registration.Handler);
            }
        }

        internal static void Clear()
        {
            lock (registrations)
                registrations.Clear();
        }

        private sealed class Registration(string modId, IBindable<ModKeys> keys, Action handler, bool onRelease)
        {
            public string ModId { get; } = modId;
            public IBindable<ModKeys> Keys { get; } = keys;
            public Action Handler { get; } = handler;
            public bool OnRelease { get; } = onRelease;
            public bool Held { get; set; }
        }
    }
}
