namespace Omamori.API
{
    public sealed record RoomDetails
    {
        /// <summary>the server's room id, rooms only exist online so there's no separate online id</summary>
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public RoomMatchType MatchType { get; init; }

        public RoomState State { get; init; }

        public int UserCount { get; init; }
        public bool HasPassword { get; init; }
    }
}
