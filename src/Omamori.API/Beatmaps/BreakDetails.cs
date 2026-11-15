using System;

namespace Omamori.API
{
    public sealed record BreakDetails
    {
        public TimeSpan StartTime { get; init; }

        public TimeSpan EndTime { get; init; }
    }
}
