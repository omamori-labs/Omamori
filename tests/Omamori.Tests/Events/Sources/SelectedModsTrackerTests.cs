using NUnit.Framework;
using Omamori.Core.Events.Sources;

namespace Omamori.Tests
{
    [TestFixture]
    public class SelectedModsTrackerTests
    {
        [Test]
        public void NoModsAtFirstIsNoChange() => Assert.That(new SelectedModsTracker().Update([]), Is.False);

        [Test]
        public void PickingAModIsAChange() => Assert.That(new SelectedModsTracker().Update(["HD"]), Is.True);

        [Test]
        public void TheSameModsReorderedAreNoChange()
        {
            var mods = new SelectedModsTracker();
            mods.Update(["HD", "DT"]);

            Assert.That(mods.Update(["DT", "HD"]), Is.False);
        }

        [Test]
        public void DroppingAModIsAChange()
        {
            var mods = new SelectedModsTracker();
            mods.Update(["HD", "DT"]);

            Assert.That(mods.Update(["HD"]), Is.True);
        }
    }
}
