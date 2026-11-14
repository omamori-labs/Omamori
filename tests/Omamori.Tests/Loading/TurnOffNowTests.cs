using System;
using System.IO;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Loading;
using Omamori.Core.Safety;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class TurnOffNowTests
    {
        [TearDown]
        public void TearDown() => CoreState.Reset();

        [Test]
        public void RunningModLeavesTheSessionStraightAway()
        {
            var mod = loaded("test.off");

            ModLoader.TurnOffNow("test.off");

            Assert.That(ModLoader.IsLoaded("test.off"), Is.False);
            Assert.That(ModLoader.TurnedOff.Select(c => c.Manifest.Id), Does.Contain("test.off"));
            Assert.That(ModFaults.IsFaulted("test.off"), Is.True);
            Assert.That(ModFaults.HasCrashed("test.off"), Is.False);
            mod.Instance.Received(1).Unload();
        }

        [Test]
        public void ModsThatNeedItGoWithItButOptionalOnesStay()
        {
            loaded("test.base");
            loaded("test.needs", new ModDependency { Id = "test.base" });
            loaded("test.likes", new ModDependency { Id = "test.base", Optional = true });

            ModLoader.TurnOffNow("test.base");

            Assert.That(ModLoader.Skipped.Single(s => s.Manifest?.Id == "test.needs").Reason, Does.Contain("test.base"));
            Assert.That(ModLoader.IsLoaded("test.likes"), Is.True);
        }

        [Test]
        public void ModsThatNeedAModThatNeedsItGoToo()
        {
            loaded("test.base");
            loaded("test.middle", new ModDependency { Id = "test.base" });
            loaded("test.top", new ModDependency { Id = "test.middle" });

            ModLoader.TurnOffNow("test.base");

            Assert.That(ModLoader.Loaded, Is.Empty);
        }

        [Test]
        public void UnloadThatThrowsStillTurnsTheModOff()
        {
            var mod = loaded("test.throws");
            mod.Instance.When(i => i.Unload()).Do(_ => throw new InvalidOperationException("broken unload"));

            ModLoader.TurnOffNow("test.throws");

            Assert.That(ModLoader.IsLoaded("test.throws"), Is.False);
            Assert.That(ModFaults.HasCrashed("test.throws"), Is.False);
        }

        [Test]
        public void ThrowFromATurnedOffModIsContainedWithoutCountingAsACrash()
        {
            var mod = loaded("test.stray");
            ModLoader.TurnOffNow("test.stray");

            Assert.That(ModFaults.Contain(mod, new InvalidOperationException("left a thread running"), scheduler: null), Is.True);
            Assert.That(ModFaults.HasCrashed("test.stray"), Is.False);
        }

        [Test]
        public void ModThatIsntRunningIsLeftAlone() => Assert.DoesNotThrow(() => ModLoader.TurnOffNow("test.not-loaded"));

        private static LoadedMod loaded(string id, params ModDependency[] dependencies)
        {
            var manifest = new ModManifest { Id = id, Name = id, Version = "1.0.0", Dependencies = [.. dependencies] };
            var mod = new LoadedMod(manifest, Substitute.For<IMod>(), ModIsolation.CreateLoader(Path.Combine(AppContext.BaseDirectory, "Semver.dll")), id, id);

            ModLoader.Add(mod);
            return mod;
        }
    }
}
