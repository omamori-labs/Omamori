using System;
using System.Collections.Generic;
using System.Linq;

namespace Omamori.API
{
    /// <summary>
    ///     one of osu!'s mods as it was set, compares by value so the same mods with different settings aren't equal
    /// </summary>
    public sealed record ModDetails
    {
        /// <summary>like HD or DT</summary>
        public string Acronym { get; init; } = string.Empty;

        /// <summary>
        ///     only the settings changed from their defaults, by osu!'s own names like speed_change
        ///     values are written the same for live plays and saved scores, like 1.5 or true, and an option as its number
        /// </summary>
        public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();

        public bool Equals(ModDetails? other) =>
            other is not null
            && Acronym == other.Acronym
            && Settings.Count == other.Settings.Count
            && Settings.All(s => other.Settings.TryGetValue(s.Key, out string? value) && value == s.Value);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Acronym);

            // order-free, the same settings in any order hash the same
            foreach (var setting in Settings.OrderBy(s => s.Key, StringComparer.Ordinal))
            {
                hash.Add(setting.Key);
                hash.Add(setting.Value);
            }

            return hash.ToHashCode();
        }

        public override string ToString() =>
            Settings.Count == 0 ? Acronym : $"{Acronym}({string.Join(", ", Settings.OrderBy(s => s.Key, StringComparer.Ordinal).Select(s => $"{s.Key}={s.Value}"))})";
    }
}
