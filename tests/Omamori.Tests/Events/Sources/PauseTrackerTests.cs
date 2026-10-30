using NUnit.Framework;
using Omamori.Core.Events.Sources;

namespace Omamori.Tests
{
    [TestFixture]
    public class PauseTrackerTests
    {
        [Test]
        public void TheClockStartingIsNotAResume()
        {
            var pauses = new PauseTracker();

            Assert.That(pauses.IsPlayerPause(paused: false, completed: false), Is.False);
        }

        [Test]
        public void PausesAfterTheStartCount()
        {
            var pauses = new PauseTracker();
            pauses.IsPlayerPause(paused: false, completed: false);

            Assert.That(pauses.IsPlayerPause(paused: true, completed: false), Is.True);
            Assert.That(pauses.IsPlayerPause(paused: false, completed: false), Is.True);
        }

        // the clock is paused while the player loads, that isn't the player pausing
        [Test]
        public void NothingCountsBeforeTheClockFirstRuns()
        {
            var pauses = new PauseTracker();

            Assert.That(pauses.IsPlayerPause(paused: true, completed: false), Is.False);
            Assert.That(pauses.IsPlayerPause(paused: true, completed: false), Is.False);
        }

        // the clock stops when the play passes or fails
        [Test]
        public void TheClockStoppingAtTheEndIsNotAPause()
        {
            var pauses = new PauseTracker();
            pauses.IsPlayerPause(paused: false, completed: false);

            Assert.That(pauses.IsPlayerPause(paused: true, completed: true), Is.False);
        }
    }
}
