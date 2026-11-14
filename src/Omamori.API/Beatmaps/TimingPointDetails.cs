namespace Omamori.API
{
    public sealed record TimingPointDetails
    {
        /// <summary>in milliseconds</summary>
        public double Time { get; init; }

        /// <summary>milliseconds per beat</summary>
        public double BeatLength { get; init; }

        public double Bpm { get; init; }

        /// <summary>beats in a bar</summary>
        public int BeatsPerBar { get; init; }
    }
}
