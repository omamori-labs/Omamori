namespace Omamori.API
{
    public sealed record BreakDetails
    {
        /// <summary>in milliseconds</summary>
        public double StartTime { get; init; }

        public double EndTime { get; init; }
    }
}
