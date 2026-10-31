using System;
using System.IO;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Loading;
using Omamori.Core.Manager;
using Omamori.Core.Safety;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class InstalledModTests
    {
        private string directory = null!;

        [SetUp]
        public void SetUp() => directory = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}");

        [TearDown]
        public void TearDown()
        {
            ModLoader.Switches = null;
            ModFaults.History = null;

            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        [Test]
        public void FailedLoadShowsItsReasonNotACrash()
        {
            var manifest = unique();
            ModFaults.TurnOff(manifest.Id);

            var mod = new InstalledMod("Broken", manifest, true, "failed to load: duplicate key glow");

            Assert.That(mod.Status, Is.EqualTo(ModStatus.Problem));
            Assert.That(mod.StatusText, Is.EqualTo("Failed to load: duplicate key glow."));
        }

        [Test]
        public void CrashWhileLoadedShowsCrashed()
        {
            var manifest = unique();
            ModFaults.TurnOff(manifest.Id);

            var mod = new InstalledMod("Crashy", manifest, true, null);

            Assert.That(mod.Status, Is.EqualTo(ModStatus.Crashed));
            Assert.That(mod.StatusText, Is.EqualTo("Crashed and was turned off for this session."));
        }

        [Test]
        public void TurningOffIsPendingUntilRelaunch()
        {
            var manifest = unique();
            ModLoader.Switches = ModSwitches.Open(Path.Combine(directory, ModSwitches.FILENAME));
            ModLoader.Switches.SetEnabled(manifest.Id, false);

            var mod = new InstalledMod("Mod", manifest, true, null);

            Assert.That(mod.Status, Is.EqualTo(ModStatus.On));
            Assert.That(mod.Pending, Is.True);
            Assert.That(mod.StatusText, Is.EqualTo("Turns off next time osu! starts."));
        }

        [Test]
        public void OnlyCrashHistorySkipsCanBeRetried()
        {
            var crashed = unique();
            string path = Path.Combine(directory, FaultHistory.FILENAME);

            FaultHistory.Open(path).Record(crashed, new InvalidOperationException("boom"));
            ModFaults.History = FaultHistory.Open(path);
            ModFaults.History.Record(crashed, new InvalidOperationException("boom"));

            Assert.That(new InstalledMod("Crashed twice", crashed, true, "crashed in 2 sessions in a row").CanRetry, Is.True);
            Assert.That(new InstalledMod("Bad manifest", unique(), true, "entry assembly missing").CanRetry, Is.False);
            Assert.That(new InstalledMod("Fine", crashed, true, null).CanRetry, Is.False);
        }

        private static ModManifest unique() => new ModManifest { Id = $"test.installed-{Guid.NewGuid():N}", Version = "1.0.0" };
    }
}
