using System.Collections.Generic;
using System.Linq;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     lazer reorders the same mods, that isn't a change
    /// </summary>
    internal sealed class SelectedModsTracker
    {
        private IReadOnlyList<string> current = [];

        public bool Update(IReadOnlyList<string> mods)
        {
            if (mods.ToHashSet().SetEquals(current))
                return false;

            current = mods;
            return true;
        }
    }
}
