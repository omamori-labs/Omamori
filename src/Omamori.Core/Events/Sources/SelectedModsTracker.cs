using System.Collections.Generic;
using System.Linq;
using Omamori.API;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     lazer reorders the same mods, that isn't a change, the first selection always is so mods learn it at launch
    ///     a setting changing is a change, mod details compare their settings
    /// </summary>
    internal sealed class SelectedModsTracker
    {
        private IReadOnlyList<ModDetails>? current;

        public bool Update(IReadOnlyList<ModDetails> mods)
        {
            if (current != null && mods.ToHashSet().SetEquals(current))
                return false;

            current = mods;
            return true;
        }
    }
}
