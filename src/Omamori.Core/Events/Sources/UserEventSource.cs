using Omamori.API;
using osu.Framework.Allocation;
using osu.Game;
using osu.Game.Online;
using osu.Game.Users;

namespace Omamori.Core
{
    /// <summary>
    ///     logging in and out, and statistics after the server processes a play
    /// </summary>
    internal static class UserEventSource
    {
        public static void Watch(OsuGame game)
        {
            var api = game.API;

            void user() => FeatureRegistry.Run(Feature.UserEvents, () =>
            {
                var local = api.LocalUser.Value;
                bool loggedIn = api.IsLoggedIn;

                GameEvents.RaiseChanged(GameEvent.UserChanged, new UserDetails
                {
                    LoggedIn = loggedIn,
                    Id = loggedIn ? local.Id : null,
                    Username = loggedIn ? local.Username : string.Empty,
                    Country = loggedIn && local.CountryCode != CountryCode.Unknown ? local.CountryCode.ToString() : string.Empty,
                });
            });

            // the api changes these from its own thread
            api.LocalUser.BindValueChanged(_ => game.Schedule(user));
            api.State.BindValueChanged(_ => game.Schedule(user));
            user();

            var statistics = game.Dependencies.Get<LocalUserStatisticsProvider>();
            statistics.StatisticsUpdated += update => game.Schedule(() => FeatureRegistry.Run(Feature.UserEvents, () =>
                GameEvents.Raise(GameEvent.StatisticsUpdated, new StatisticsUpdate
                {
                    Ruleset = update.Ruleset.ShortName,
                    Before = update.OldStatistics is { } before ? EventDetails.Statistics(before) : null,
                    After = EventDetails.Statistics(update.NewStatistics),
                })));
        }
    }
}
