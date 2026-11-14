using System;
using osu.Game.Rulesets.Mods;

namespace Omamori.API
{
    /// <summary>
    ///     osu!'s own mods, the ones in the mod select overlay
    ///     a play using a mod added here is never submitted, osu! wouldn't know what it is
    /// </summary>
    public interface IGameMods
    {
        /// <summary>
        ///     adds a mod to mod select, mod select gets new instances every time the ruleset changes
        ///     <paramref name="rulesets"/> are short names like "osu" or "mania", none means every ruleset
        /// </summary>
        void Add<T>(params string[] rulesets) where T : Mod, new();

        /// <summary>
        ///     <see cref="Add{T}"/> with a factory of your own, so every instance can share state like the current beatmap
        ///     mod settings are bindables, a factory that hands each instance the same range bindables lets the range follow the map
        ///     the factory can run on any thread, lazer also lists mods while importing scores
        /// </summary>
        void Add(Func<Mod> factory, params string[] rulesets);
    }
}
