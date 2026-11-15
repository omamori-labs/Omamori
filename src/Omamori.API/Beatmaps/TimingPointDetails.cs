using System;

namespace Omamori.API
{
    public sealed record TimingPointDetails
    {
        public TimeSpan Time { get; init; }

        /// <summary>how long one beat lasts</summary>
        public TimeSpan BeatLength { get; init; }

        public double Bpm { get; init; }

        /// <summary>beats in a bar</summary>
        public int BeatsPerBar { get; init; }
    }
}
