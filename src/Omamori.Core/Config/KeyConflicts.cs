using System.Collections.Generic;
using System.Linq;
using Omamori.API;

namespace Omamori.Core.Config
{
    /// <summary>
    ///     only a warning
    /// </summary>
    internal static class KeyConflicts
    {
        public const int SHOWN = 3;

        public static IReadOnlyList<string> Find(ModKeys keys, string modId, string key, IEnumerable<KeyUse> uses)
        {
            if (keys.IsNone)
                return [];

            var mine = KeyCapture.Normalise(keys);

            return
            [
                .. uses.Where(u => u.Setting != (modId, key) && KeyCapture.Normalise(u.Keys).Equals(mine))
                       .Select(u => u.Owner)
                       .Distinct(),
            ];
        }

        public static string? Note(IReadOnlyList<string> owners)
        {
            if (owners.Count == 0)
                return null;

            string shown = string.Join(", ", owners.Take(SHOWN));
            return owners.Count > SHOWN ? $"Also used by {shown} and {owners.Count - SHOWN} more." : $"Also used by {shown}.";
        }
    }
}
