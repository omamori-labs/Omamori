using Omamori.API;
using osu.Game;

namespace Omamori.Core
{
    /// <summary>
    ///     RoomUpdated fires for every ready state and so on, comparing the room's summary leaves joins, leaves and real changes
    /// </summary>
    internal static class MultiplayerEventSource
    {
        private static RoomDetails? current;

        public static void Watch(OsuGame game)
        {
            var client = game.MultiplayerClient;

            client.RoomUpdated += () => game.Schedule(() => FeatureRegistry.Run(Feature.MultiplayerEvents, () =>
            {
                var room = client.Room is { } r
                    ? new RoomDetails
                    {
                        Id = r.RoomID,
                        Name = r.Settings.Name,
                        MatchType = r.Settings.MatchType.ToString(),
                        State = r.State.ToString(),
                        Users = r.Users.Count,
                        HasPassword = r.Settings.Password.Length > 0,
                    }
                    : null;

                if (room == current)
                    return;

                var kind = current == null ? RoomChangeKind.Joined : room == null ? RoomChangeKind.Left : RoomChangeKind.Updated;
                var reported = (room ?? current)!;

                current = room;
                GameEvents.Raise(GameEvent.RoomChanged, new RoomChange { Kind = kind, Room = reported });
            }));
        }
    }
}
