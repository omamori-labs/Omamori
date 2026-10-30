using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Events.Sources;

namespace Omamori.Tests
{
    [TestFixture]
    public class PlayTrackerTests
    {
        private static readonly PlayDetails first = new PlayDetails { Ruleset = "osu" };
        private static readonly PlayDetails second = new PlayDetails { Ruleset = "mania" };

        [Test]
        public void StartThenEndGivesThePlay()
        {
            var plays = new PlayTracker();

            Assert.That(plays.Start(first), Is.Null);
            Assert.That(plays.End(), Is.SameAs(first));
        }

        // results exiting resumes the player, which exits again
        [Test]
        public void EndingTwiceOnlyEndsOnce()
        {
            var plays = new PlayTracker();
            plays.Start(first);
            plays.End();

            Assert.That(plays.End(), Is.Null);
        }

        [Test]
        public void EndingWithNothingRunningIsNothing() => Assert.That(new PlayTracker().End(), Is.Null);

        // a retry pushes a new player before the old one is seen exiting
        [Test]
        public void StartingOverARunningPlayHandsItBackToEnd()
        {
            var plays = new PlayTracker();
            plays.Start(first);

            Assert.That(plays.Start(second), Is.SameAs(first));
            Assert.That(plays.End(), Is.SameAs(second));
        }
    }
}
