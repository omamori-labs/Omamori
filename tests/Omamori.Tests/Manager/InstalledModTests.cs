using System;
using System.IO;
using System.Linq;
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
        public void SetUp() => directory = TempDirectory.New();

        [TearDown]
        public void TearDown()
        {
            CoreState.Reset();
            TempDirectory.Delete(directory);
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

        [Test]
        public void OnlyModsDeclaringPatchesAreMarkedAsPatching()
        {
            var patching = unique();
            patching.Capabilities = [ModCapability.Cosmetic, ModCapability.Patches];

            Assert.That(new InstalledMod("Patching", patching, true, null).Patches, Is.True);
            Assert.That(new InstalledMod("Cosmetic", unique(), true, null).Patches, Is.False);
            Assert.That(new InstalledMod("Unreadable", null, false, "unreadable mod.json").Patches, Is.False);
        }

        [Test]
        public void ProblemsFilterShowsCrashedAndFailedModsOnly()
        {
            var crashedManifest = unique();
            ModFaults.TurnOff(crashedManifest.Id);

            var crashed = new InstalledMod("Crashy", crashedManifest, true, null);
            var failed = new InstalledMod("Broken", unique(), true, "entry assembly missing");
            var on = new InstalledMod("Fine", unique(), true, null);
            var off = new InstalledMod("Off", unique(), false, null);

            InstalledMod[] mods = [crashed, failed, on, off];
            InstalledMod[] problems = [crashed, failed];

            Assert.That(mods.Where(m => m.Matches(ModFilter.Problems)), Is.EqualTo(problems));
            Assert.That(mods.Single(m => m.Matches(ModFilter.On)), Is.SameAs(on));
            Assert.That(mods.Single(m => m.Matches(ModFilter.Off)), Is.SameAs(off));
            Assert.That(mods.Where(m => m.Matches(ModFilter.All)), Is.EqualTo(mods));
        }

        [Test]
        public void SearchNeedsEveryTermButIgnoresCase()
        {
            var manifest = unique();
            manifest.Author = "catforgor";
            manifest.Description = "shows the current song";

            var mod = new InstalledMod("Now Playing", manifest, true, null);

            Assert.That(mod.Matches(["PLAYING", "catforgor"]), Is.True);
            Assert.That(mod.Matches(["playing", "skin"]), Is.False);
            Assert.That(mod.Matches([]), Is.True);
        }

        private static ModManifest unique() => new ModManifest { Id = $"test.installed-{Guid.NewGuid():N}", Version = "1.0.0" };
    }
}
