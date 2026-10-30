namespace Omamori.API
{
    public sealed record RoomChange
    {
        public RoomChangeKind Kind { get; init; }

        /// <summary>the room after the change, the one just left for <see cref="RoomChangeKind.Left"/></summary>
        public RoomDetails Room { get; init; } = new RoomDetails();
    }
}
