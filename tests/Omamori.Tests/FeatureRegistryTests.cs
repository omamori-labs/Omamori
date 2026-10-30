using System;
using HarmonyLib;
using NUnit.Framework;
using Omamori.Core;

namespace Omamori.Tests
{
    /// <summary>
    ///     real harmony patches against methods in this assembly
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class FeatureRegistryTests
    {
        private Harmony harmony = null!;

        [SetUp]
        public void SetUp()
        {
            harmony = new Harmony($"omamori.tests.{Guid.NewGuid()}");
            Target.Log = string.Empty;
        }

        [TearDown]
        public void TearDown()
        {
            harmony.UnpatchAll(harmony.Id);
            FeatureRegistry.Set(Feature.MainMenuUi, FeatureState.Pending, null);
        }

        [Test]
        public void FaultIsAnnouncedOnce()
        {
            FeatureRegistry.Set(Feature.MainMenuUi, FeatureState.Active, null);

            int announced = 0;
            void count(Feature f) => announced += f == Feature.MainMenuUi ? 1 : 0;
            FeatureRegistry.Faulted += count;

            try
            {
                FeatureRegistry.Run(Feature.MainMenuUi, () => throw new InvalidOperationException("boom"));
                FeatureRegistry.Run(Feature.MainMenuUi, () => throw new InvalidOperationException("again"));
            }
            finally
            {
                FeatureRegistry.Faulted -= count;
            }

            Assert.That(announced, Is.EqualTo(1));
        }

        [Test]
        public void AppliesEveryClass()
        {
            var result = FeatureRegistry.Apply(harmony, [typeof(PrefixA), typeof(PostfixB)]);

            Assert.That(result.Error, Is.Null);
            Assert.That(result.Applied, Is.EqualTo(2));
            Assert.That(run(), Is.EqualTo("[a][run][b]"));
        }

        // the "lazer renamed something" case harmony's own UnpatchCategory can't recover from
        [Test]
        public void PartialFailureRollsBackWhatDidApply()
        {
            var result = FeatureRegistry.Apply(harmony, [typeof(PostfixB), typeof(MissingTarget)]);

            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Applied, Is.EqualTo(1));
            Assert.That(result.RolledBack, Is.EqualTo(1));
            Assert.That(run(), Is.EqualTo("[run]"));
        }

        // mod-loading and global-overlay patch the same method, rolling one back must not touch the other
        [Test]
        public void RollbackLeavesOtherFeaturesOnTheSameMethod()
        {
            FeatureRegistry.Apply(harmony, [typeof(PrefixA)]);
            FeatureRegistry.Apply(harmony, [typeof(PostfixB), typeof(MissingTarget)]);

            Assert.That(run(), Is.EqualTo("[a][run]"));
        }

        [Test]
        public void FailureAfterPatchingRollsBack()
        {
            var result = FeatureRegistry.Apply(harmony, [typeof(PrefixA)], () => throw new InvalidOperationException("injected"));

            Assert.That(result.Error, Is.TypeOf<InvalidOperationException>());
            Assert.That(result.RolledBack, Is.EqualTo(1));
            Assert.That(run(), Is.EqualTo("[run]"));
        }

        [Test]
        public void RunSkipsInactiveFeatures()
        {
            bool called = false;
            int result = FeatureRegistry.Run(Feature.MainMenuUi, 1, v =>
            {
                called = true;
                return v + 1;
            });

            Assert.That(called, Is.False);
            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void RunTransformsWhenActive()
        {
            FeatureRegistry.Set(Feature.MainMenuUi, FeatureState.Active, null);
            Assert.That(FeatureRegistry.Run(Feature.MainMenuUi, 1, v => v + 1), Is.EqualTo(2));
        }

        // a throw in patch code turns the feature off and hands back the original value, never reaching the game
        [Test]
        public void ThrowingTransformFaultsTheFeature()
        {
            FeatureRegistry.Set(Feature.MainMenuUi, FeatureState.Active, null);

            int result = FeatureRegistry.Run<int>(Feature.MainMenuUi, 1, _ => throw new InvalidOperationException("boom"));

            Assert.That(result, Is.EqualTo(1));
            Assert.That(FeatureRegistry.IsActive(Feature.MainMenuUi), Is.False);
        }

        private static string run()
        {
            Target.Log = string.Empty;
            Target.Run();
            return Target.Log;
        }

        public static class Target
        {
            public static string Log { get; set; } = string.Empty;

            // no inlining, a patch on an inlined method wouldn't run
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            public static void Run() => Log += "[run]";
        }

        [HarmonyPatch(typeof(Target), nameof(Target.Run))]
        private static class PrefixA
        {
            [HarmonyPrefix]
            private static void prefix() => Target.Log += "[a]";
        }

        [HarmonyPatch(typeof(Target), nameof(Target.Run))]
        private static class PostfixB
        {
            [HarmonyPostfix]
            private static void postfix() => Target.Log += "[b]";
        }

        [HarmonyPatch(typeof(Target), "DoesNotExist")]
        private static class MissingTarget
        {
            [HarmonyPostfix]
            private static void postfix()
            {
            }
        }
    }
}
