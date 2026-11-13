using System;
using osu.Game.Rulesets.Mods;

namespace Omamori.Core.Mods
{
    internal sealed record ModRegistration(string ModId, string[] Rulesets, Func<Mod> Factory);
}
