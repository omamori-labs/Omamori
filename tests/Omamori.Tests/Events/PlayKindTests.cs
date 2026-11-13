using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Events;
using osu.Game.Screens.OnlinePlay.DailyChallenge;
using osu.Game.Screens.OnlinePlay.Multiplayer;
using osu.Game.Screens.OnlinePlay.Multiplayer.Spectate;
using osu.Game.Screens.OnlinePlay.Playlists;
using osu.Game.Screens.Play;

namespace Omamori.Tests
{
    [TestFixture]
    public class PlayKindTests
    {
        [TestCase(typeof(SoloPlayer), PlayKind.Solo)]
        [TestCase(typeof(PlaylistsPlayer), PlayKind.Playlist)]
        [TestCase(typeof(DailyChallengePlayer), PlayKind.Playlist)]
        [TestCase(typeof(MultiplayerPlayer), PlayKind.Multiplayer)]
        [TestCase(typeof(ReplayPlayer), PlayKind.Replay)]
        [TestCase(typeof(SpectatorPlayer), PlayKind.Spectator)]
        [TestCase(typeof(SoloSpectatorPlayer), PlayKind.Spectator)]
        [TestCase(typeof(MultiSpectatorPlayer), PlayKind.Spectator)]
        [TestCase(typeof(string), PlayKind.Other)]
        public void PlayerTypeMapsToItsKind(System.Type player, PlayKind expected) => Assert.That(EventDetails.KindOf(player), Is.EqualTo(expected));
    }
}
