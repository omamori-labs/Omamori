using Omamori.API;
using Omamori.Core.Features;
using osu.Game;

namespace Omamori.Core.Events.Sources
{
    internal static class MultiplayerEventSource
    {
        private static readonly RoomTracker room = new RoomTracker();

        public static void Watch(OsuGame game)
        {
            var client = game.MultiplayerClient;

            client.RoomUpdated += () => game.Schedule(() => FeatureRegistry.Run(Feature.MultiplayerEvents, () =>
            {
                var details = client.Room is { } r
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

                if (room.Update(details) is { } change)
                    GameEvents.Raise(GameEvent.RoomChanged, change);
            }));
        }
    }
}
