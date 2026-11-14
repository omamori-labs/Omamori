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
        private const string broken_check = "omamori couldn't check it for mods added by mods";

        private static readonly List<ModRegistration> registrations = [];

        // every type a factory has handed out, so a play using one can be recognised
        private static readonly HashSet<Type> made = [];

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

            // lazer keeps each ruleset's first full mod list, and a player whose mods aren't in it refuses to load
            Ruleset.mod_reference_cache.Clear();
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

            foreach (var registration in snapshot.Where(r => appliesTo(r, ruleset)))
            {
                ModFaults.RunForMod(registration.ModId, "mod factory", () =>
                {
                    var mod = registration.Factory();

                    lock (made)
                        made.Add(mod.GetType());

                    mods.Add(mod);
                });
            }

            return mods;
        }

        /// <summary>
        ///     the ruleset's mod select lists with the registered mods added, a mod already listed isn't added twice
        /// </summary>
        public static Dictionary<ModType, IReadOnlyList<Mod>> AddTo(Dictionary<ModType, IReadOnlyList<Mod>> available, Ruleset ruleset)
        {
            var extra = For(ruleset);

            if (extra.Count == 0)
                return available;

            var lists = available.ToDictionary(p => p.Key, p => p.Value.ToList());

            foreach (var mod in extra)
            {
                if (!lists.TryGetValue(mod.Type, out var list))
                    lists[mod.Type] = list = [];

                if (list.TrueForAll(m => m.GetType() != mod.GetType()))
                    list.Add(mod);
            }

            return lists.ToDictionary(p => p.Key, p => (IReadOnlyList<Mod>)p.Value);
        }

        /// <summary>
        ///     why a play with these mods mustn't be submitted, null when none came from a mod
        ///     if the check breaks, plays aren't submitted while any mod has added a mod
        /// </summary>
        public static string? Unsubmitted(IEnumerable<Mod> mods)
        {
            bool anyRegistered;

            lock (registrations)
                anyRegistered = registrations.Count > 0;

            return FeatureRegistry.Run(Feature.GameMods, anyRegistered ? broken_check : null, _ =>
            {
                lock (made)
                    return mods.FirstOrDefault(m => made.Contains(m.GetType())) is { } mod ? $"{mod.Name} was added by a mod" : null;
            });
        }

        internal static void Clear()
        {
            lock (registrations)
                registrations.Clear();

            lock (made)
                made.Clear();
        }

        private static bool appliesTo(ModRegistration registration, Ruleset ruleset) =>
            ruleset.ShortName != CoreEntry.HostRulesetShortName
            && (registration.Rulesets.Length == 0 || registration.Rulesets.Contains(ruleset.ShortName, StringComparer.OrdinalIgnoreCase));
    }
}
