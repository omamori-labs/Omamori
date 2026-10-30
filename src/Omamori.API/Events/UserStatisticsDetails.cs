namespace Omamori.API
{
    public sealed record UserStatisticsDetails
    {
        public double? Pp { get; init; }
        public int? GlobalRank { get; init; }
        public int? CountryRank { get; init; }

        /// <summary>0 to 1</summary>
        public double Accuracy { get; init; }

        public int PlayCount { get; init; }
        public long RankedScore { get; init; }
    }
}
