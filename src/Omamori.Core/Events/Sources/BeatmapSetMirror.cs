using System.Collections.Generic;
using System.Linq;
using Omamori.API;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     a copy of the library in realm's order, realm's deletion indices point at rows that are already gone
    ///     deletions use the old indices, insertions and modifications the new ones, the same as realm's change sets
    /// </summary>
    internal sealed class BeatmapSetMirror
    {
        private List<BeatmapSetDetails> sets = [];

        public int Count => sets.Count;

        public void Reset(IEnumerable<BeatmapSetDetails> all) => sets = [.. all];

        public IReadOnlyList<BeatmapSetDetails> Remove(IEnumerable<int> deletedIndices)
        {
            var removed = new List<BeatmapSetDetails>();

            // from the end, so earlier indices stay valid
            foreach (int i in deletedIndices.OrderDescending())
            {
                removed.Add(sets[i]);
                sets.RemoveAt(i);
            }

            removed.Reverse();
            return removed;
        }

        public void Insert(IEnumerable<(int Index, BeatmapSetDetails Set)> inserted)
        {
            foreach (var (index, set) in inserted.OrderBy(i => i.Index))
                sets.Insert(index, set);
        }

        public void Replace(IEnumerable<(int Index, BeatmapSetDetails Set)> modified)
        {
            foreach (var (index, set) in modified)
                sets[index] = set;
        }
    }
}
