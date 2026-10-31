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
        private readonly IModLogger log;
        private readonly HashSet<Hook> hinted = [];

        public ModInput(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            this.log = log;
        }

        public void OnPressed(IBindable<ModKeys> keys, Action handler) => register(keys, handler, onRelease: false);

        public void OnReleased(IBindable<ModKeys> keys, Action handler) => register(keys, handler, onRelease: true);

        private void register(IBindable<ModKeys> keys, Action handler, bool onRelease)
        {
            if (!HookCheck.CanRegister(manifest, log, hinted, Hook.KeyBindings, "key binding"))
                return;

            lock (registrations)
                registrations.Add(new Registration(manifest.Id, keys.GetBoundCopy(), handler, onRelease));
        }

        /// <summary>
        ///     update thread only, exact matches so ctrl+k doesn't also fire k
        /// </summary>
        public static void KeysChanged(KeyCombination pressed, InputState state)
        {
            List<Registration> all;

            lock (registrations)
                all = [.. registrations];

            foreach (var registration in all)
            {
                var binding = registration.Keys.Value;
                bool held = binding is { IsNone: false } && binding.ToCombination().IsPressed(pressed, state, KeyCombinationMatchingMode.Exact);

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
