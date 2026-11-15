using System.Collections.Generic;
using System.Linq;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     lazer reorders the same mods, that isn't a change, the first selection always is so mods learn it at launch
    /// </summary>
    internal sealed class SelectedModsTracker
    {
        private IReadOnlyList<string>? current;

        public bool Update(IReadOnlyList<string> mods)
        {
            if (current != null && mods.ToHashSet().SetEquals(current))
                return false;

            current = mods;
            return true;
        }
    }
}
