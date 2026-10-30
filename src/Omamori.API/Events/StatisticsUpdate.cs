namespace Omamori.API
{
    public sealed record StatisticsUpdate
    {
        public string Ruleset { get; init; } = string.Empty;

        /// <summary>null the first time statistics arrive this session</summary>
        public UserStatisticsDetails? Before { get; init; }

        public UserStatisticsDetails After { get; init; } = new UserStatisticsDetails();
    }
}
