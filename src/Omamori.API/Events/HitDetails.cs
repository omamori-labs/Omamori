namespace Omamori.API
{
    public sealed record HitDetails
    {
        /// <summary>when the object was meant to be hit, in ms from the start of the map</summary>
        public double Time { get; init; }

        /// <summary>how early (negative) or late the hit was, in ms</summary>
        public double Offset { get; init; }

        /// <summary>Great, Ok, Meh, Miss and so on</summary>
        public string Result { get; init; } = string.Empty;

        /// <summary>where it was hit relative to the object, for rulesets with positions</summary>
        public float? X { get; init; }

        public float? Y { get; init; }
    }
}
