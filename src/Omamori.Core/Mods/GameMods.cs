using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace Omamori.Core.Mods
{
    internal sealed class GameMods : IGameMods
    {
        private static readonly List<ModRegistration> registrations = [];

        private readonly ModManifest manifest;
        private readonly HookCheck hooks;

        public GameMods(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            hooks = new HookCheck(manifest, log);
        }

        public void Add<T>(params string[] rulesets) where T : Mod, new() => Add(() => new T(), rulesets);

        public void Add(Func<Mod> factory, params string[] rulesets)
        {
            if (!hooks.CanRegister(Hook.GameMods, "mod"))
                return;

            lock (registrations)
                registrations.Add(new ModRegistration(manifest.Id, [.. rulesets], factory));
        }

        /// <summary>
        ///     a factory that throws turns its mod off
        /// </summary>
        public static IReadOnlyList<Mod> For(Ruleset ruleset)
        {
            ModRegistration[] snapshot;

            lock (registrations)
                snapshot = [.. registrations];

            var mods = new List<Mod>();

            foreach (var registration in snapshot.Where(r => appliesTo(r, ruleset) && !ModFaults.IsFaulted(r.ModId)))
            {
                ModFaults.RunForMod(registration.ModId, "mod factory", () => mods.Add(registration.Factory()));
            }

            return mods;
        }

        internal static void Clear()
        {
            lock (registrations)
                registrations.Clear();
        }

        private static bool appliesTo(ModRegistration registration, Ruleset ruleset) =>
            ruleset.ShortName != CoreEntry.HostRulesetShortName
            && (registration.Rulesets.Length == 0 || registration.Rulesets.Contains(ruleset.ShortName, StringComparer.OrdinalIgnoreCase));
    }
}
