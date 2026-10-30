namespace Omamori.API
{
    public sealed record RoomDetails
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;

        /// <summary>HeadToHead, TeamVersus and so on</summary>
        public string MatchType { get; init; } = string.Empty;

        /// <summary>Open, WaitingForLoad, Playing, Closed</summary>
        public string State { get; init; } = string.Empty;

        public int Users { get; init; }
        public bool HasPassword { get; init; }
    }
}
