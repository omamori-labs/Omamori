using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core;
using osu.Framework.Graphics.Containers;
using Semver;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ModFaultsTests
    {
        private static readonly Assembly tests = typeof(ModFaultsTests).Assembly;

        [TearDown]
        public void TearDown() => FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Pending, null);

        [Test]
        public void ThrowSiteIsBlamedOverItsCaller()
        {
            var thrown = TestSetup.Caught(() => SemVersion.Parse("not a version", SemVersionStyles.Strict));
            string? owner = ModFaults.Blame(thrown, a => a == tests ? "caller" : a == typeof(SemVersion).Assembly ? "thrower" : null);

            Assert.That(owner, Is.EqualTo("thrower"));
        }

        [Test]
        public void NothingOwnedIsNotBlamed() => Assert.That(ModFaults.Blame(TestSetup.Caught(throwHere), _ => (string?)null), Is.Null);

        [Test]
        public void ExceptionsOutsideModsAreLeftToOsu() => Assert.That(ModFaults.TryContain(TestSetup.Caught(throwHere), scheduler: null), Is.False);

        [Test]
        public void ThrownFromAModsLoadContextFindsThatMod()
        {
            var mod = loaded("test.from-context");
            var semver = mod.Loader.LoadDefaultAssembly();
            var parse = semver.GetType("Semver.SemVersion")!.GetMethod("Parse", [typeof(string), semver.GetType("Semver.SemVersionStyles")!, typeof(int)])!;

            ModLoader.LOADED.Add(mod);

            try
            {
                var thrown = TestSetup.Caught(() => parse.Invoke(null, ["not a version", 0, 1024]));

                Assert.That(ModFaults.TryContain(thrown, scheduler: null), Is.True);
                Assert.That(ModFaults.IsFaulted(mod.Manifest.Id), Is.True);
            }
            finally
            {
                ModLoader.LOADED.Remove(mod);
            }
        }

        [Test]
        public void ContainedModIsTurnedOff()
        {
            var mod = loaded("test.contained");

            Assert.That(ModFaults.Contain(mod, new InvalidOperationException("boom"), scheduler: null), Is.True);
            Assert.That(ModFaults.IsFaulted(mod.Manifest.Id), Is.True);
        }

        [Test]
        public void FollowUpExceptionsInTheGraceWindowAreSwallowed()
        {
            var mod = loaded("test.grace");

            ModFaults.Contain(mod, new InvalidOperationException("first"), scheduler: null);
            Assert.That(ModFaults.Contain(mod, new InvalidOperationException("second"), scheduler: null), Is.True);
        }

        [Test]
        public void ContainingRemovesTheModsUi()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Active, null);

            var mod = loaded("test.ui-removal");
            var overlay = new Container();
            new UiHooks(mod.Manifest, new ModLogger(mod.Manifest.Id)).AddGlobalOverlay(() => new Container());

            UiHooks.Populate(API.Hook.GlobalOverlay, overlay);
            Assert.That(overlay.Count, Is.EqualTo(1));

            ModFaults.Contain(mod, new InvalidOperationException("boom"), scheduler: null);
            Assert.That(overlay.Count, Is.Zero);
        }

        [Test]
        public void TurnedOffModsUiIsNotCreatedAgain()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Active, null);

            var mod = loaded("test.no-recreate");
            new UiHooks(mod.Manifest, new ModLogger(mod.Manifest.Id)).AddGlobalOverlay(() => new Container());
            ModFaults.Contain(mod, new InvalidOperationException("boom"), scheduler: null);

            var overlay = new Container();
            UiHooks.Populate(API.Hook.GlobalOverlay, overlay);

            Assert.That(overlay.Count, Is.Zero);
        }

        [Test]
        public void PatchRemovalOnlyTakesWhatMatches()
        {
            var original = typeof(Target).GetMethod(nameof(Target.Run))!;
            var keep = new Harmony($"omamori.tests.keep.{Guid.NewGuid()}");
            var remove = new Harmony($"omamori.tests.remove.{Guid.NewGuid()}");

            try
            {
                keep.Patch(original, prefix: new HarmonyMethod(typeof(Prefixes), nameof(Prefixes.Keep)));
                remove.Patch(original, prefix: new HarmonyMethod(typeof(Prefixes), nameof(Prefixes.Remove)));

                Assert.That(PatchRemoval.Remove([original], p => p.owner == remove.Id), Is.EqualTo(1));
                Assert.That(Harmony.GetPatchInfo(original)!.Prefixes.Select(p => p.owner), Is.EqualTo((string[])[keep.Id]));
            }
            finally
            {
                keep.UnpatchAll(keep.Id);
                remove.UnpatchAll(remove.Id);
            }
        }

        [Test]
        public void ModCodeThatThrowsDoesNotEscape() =>
            Assert.DoesNotThrow(() => ModFaults.RunForMod("test.unknown", "button", () => throw new InvalidOperationException("broken mod")));

        [Test]
        public void FaultedModCodeDoesNotRun()
        {
            var mod = loaded("test.runformod");
            ModFaults.Contain(mod, new InvalidOperationException("first"), scheduler: null);

            bool ran = false;
            ModFaults.RunForMod(mod.Manifest.Id, "handler", () => ran = true);

            Assert.That(ran, Is.False);
        }

        private static LoadedMod loaded(string id)
        {
            var manifest = new ModManifest { Id = id, Name = id, Version = "1.0.0" };
            return new LoadedMod(manifest, null!, ModIsolation.CreateLoader(Path.Combine(AppContext.BaseDirectory, "Semver.dll")));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void throwHere() => throw new InvalidOperationException("from the tests");

        public static class Target
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static int Run() => 1;
        }

        public static class Prefixes
        {
            public static void Keep()
            {
            }

            public static void Remove()
            {
            }
        }
    }
}
