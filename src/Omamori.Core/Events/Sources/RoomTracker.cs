using Omamori.API;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     lazer reports every ready state and so on, comparing the room's summary leaves joins, leaves and real changes
    /// </summary>
    internal sealed class RoomTracker
    {
        private RoomDetails? current;

        /// <summary>null when nothing a mod would care about changed</summary>
        public RoomChange? Update(RoomDetails? room)
        {
            if (room == current)
                return null;

            var kind = current == null ? RoomChangeKind.Joined : room == null ? RoomChangeKind.Left : RoomChangeKind.Updated;
            var reported = (room ?? current)!;

            current = room;
            return new RoomChange { Kind = kind, Room = reported };
        }
    }
}
