using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Events.Sources;

namespace Omamori.Tests
{
    [TestFixture]
    public class RoomTrackerTests
    {
        private static readonly RoomDetails room = new RoomDetails { Id = 1, Name = "room", State = "Open", Users = 1 };

        [Test]
        public void FirstRoomIsAJoin()
        {
            var change = new RoomTracker().Update(room);

            Assert.That(change?.Kind, Is.EqualTo(RoomChangeKind.Joined));
            Assert.That(change?.Room, Is.EqualTo(room));
        }

        // lazer reports ready states and the like, they don't change the summary
        [Test]
        public void TheSameRoomAgainIsNothing()
        {
            var rooms = new RoomTracker();
            rooms.Update(room);

            Assert.That(rooms.Update(room with { }), Is.Null);
        }

        [Test]
        public void AChangedRoomIsAnUpdate()
        {
            var rooms = new RoomTracker();
            rooms.Update(room);

            var change = rooms.Update(room with { Users = 2 });

            Assert.That(change?.Kind, Is.EqualTo(RoomChangeKind.Updated));
            Assert.That(change?.Room.Users, Is.EqualTo(2));
        }

        [Test]
        public void LeavingReportsTheRoomLeft()
        {
            var rooms = new RoomTracker();
            rooms.Update(room);

            var change = rooms.Update(null);

            Assert.That(change?.Kind, Is.EqualTo(RoomChangeKind.Left));
            Assert.That(change?.Room, Is.EqualTo(room));
        }

        [Test]
        public void NoRoomWhileOutOfOneIsNothing() => Assert.That(new RoomTracker().Update(null), Is.Null);

        [Test]
        public void RejoiningAfterLeavingIsAJoin()
        {
            var rooms = new RoomTracker();
            rooms.Update(room);
            rooms.Update(null);

            Assert.That(rooms.Update(room)?.Kind, Is.EqualTo(RoomChangeKind.Joined));
        }
    }
}
