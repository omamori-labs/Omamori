using System;
using osu.Game.Rulesets.Mods;

namespace Omamori.API
{
    /// <summary>
    ///     osu!'s own mods, the ones in the mod select overlay
    /// </summary>
    public interface IGameMods
    {
        /// <summary>
        ///     adds a mod to mod select, a new instance is made every time a ruleset lists its mods
        ///     <paramref name="rulesets"/> are short names like "osu" or "mania", none means every ruleset
        /// </summary>
        void Add<T>(params string[] rulesets) where T : Mod, new();

        /// <summary>
        ///     <see cref="Add{T}"/> with a factory of your own, so every instance can share state like the current beatmap
        ///     mod settings are bindables, a factory that hands each instance the same range bindables lets the range follow the map
        /// </summary>
        void Add(Func<Mod> factory, params string[] rulesets);
    }
}
