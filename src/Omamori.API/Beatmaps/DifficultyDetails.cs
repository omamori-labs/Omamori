namespace Omamori.API
{
    public sealed record DifficultyDetails
    {
        public double StarRating { get; init; }

        public int MaxCombo { get; init; }

        /// <summary>the ruleset it was worked out for</summary>
        public string Ruleset { get; init; } = string.Empty;
    }
}
