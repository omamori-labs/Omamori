using System.Collections.Generic;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Events.Sources;

namespace Omamori.Tests
{
    [TestFixture]
    public class SelectedModsTrackerTests
    {
        private static readonly ModDetails hd = new ModDetails { Acronym = "HD" };
        private static readonly ModDetails dt = new ModDetails { Acronym = "DT" };

        [Test]
        public void FirstSelectionIsAChangeEvenWithNoMods() => Assert.That(new SelectedModsTracker().Update([]), Is.True);

        [Test]
        public void NoModsAgainIsNoChange()
        {
            var mods = new SelectedModsTracker();
            mods.Update([]);

            Assert.That(mods.Update([]), Is.False);
        }

        [Test]
        public void PickingAModIsAChange() => Assert.That(new SelectedModsTracker().Update([hd]), Is.True);

        [Test]
        public void TheSameModsReorderedAreNoChange()
        {
            var mods = new SelectedModsTracker();
            mods.Update([hd, dt]);

            Assert.That(mods.Update([dt, hd]), Is.False);
        }

        [Test]
        public void DroppingAModIsAChange()
        {
            var mods = new SelectedModsTracker();
            mods.Update([hd, dt]);

            Assert.That(mods.Update([hd]), Is.True);
        }

        [Test]
        public void ChangingASettingIsAChange()
        {
            var mods = new SelectedModsTracker();
            mods.Update([dt with { Settings = new Dictionary<string, string> { ["speed_change"] = "1.25" } }]);

            Assert.That(mods.Update([dt with { Settings = new Dictionary<string, string> { ["speed_change"] = "1.5" } }]), Is.True);
        }

        [Test]
        public void TheSameSettingsAgainAreNoChange()
        {
            var mods = new SelectedModsTracker();
            mods.Update([dt with { Settings = new Dictionary<string, string> { ["speed_change"] = "1.5" } }]);

            Assert.That(mods.Update([dt with { Settings = new Dictionary<string, string> { ["speed_change"] = "1.5" } }]), Is.False);
        }
    }
}
