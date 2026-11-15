using System;

namespace Omamori.API
{
    public sealed record HitObjectDetails
    {
        public TimeSpan StartTime { get; init; }

        /// <summary>the same as <see cref="StartTime"/> for objects without a length, like circles</summary>
        public TimeSpan EndTime { get; init; }

        /// <summary>the ruleset's own name for it, like HitCircle, Slider, Hit or HoldNote</summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>playfield position, null for rulesets without one, osu! is 512 by 384</summary>
        public float? X { get; init; }

        /// <summary>null for rulesets without one, catch objects only have <see cref="X"/></summary>
        public float? Y { get; init; }

        public bool IsNewCombo { get; init; }
    }
}
