using System;
using System.Collections.Generic;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     the score processor only appends hit events, or drops the last ones when a replay seeks back
    /// </summary>
    internal sealed class HitCursor
    {
        private int seen;

        public List<T> Unseen<T>(IReadOnlyList<T> hits)
        {
            int from = Math.Min(seen, hits.Count);
            seen = hits.Count;

            var unseen = new List<T>(hits.Count - from);

            for (int i = from; i < hits.Count; i++)
                unseen.Add(hits[i]);

            return unseen;
        }

        public void Rewind(int count) => seen = Math.Min(seen, count);
    }
}
