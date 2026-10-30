using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using HarmonyLib;
using NUnit.Framework;
using Omamori.Core;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class CrashAttributionTests
    {
        private static readonly Assembly tests = typeof(CrashAttributionTests).Assembly;

        [Test]
        public void ThrownFromOurCodeIsOurs() =>
            Assert.That(CrashAttribution.Involvement(caught(throwHere), a => a == tests), Is.EqualTo(tests.GetName().Name));

        [Test]
        public void ThrownFromElsewhereIsNotOurs() =>
            Assert.That(CrashAttribution.Involvement(caught(() => int.Parse("x", System.Globalization.CultureInfo.InvariantCulture)), _ => false), Is.Null);

        [Test]
        public void NoExceptionIsNotOurs() => Assert.That(CrashAttribution.Involvement(null), Is.Null);

        [Test]
        public void InnerExceptionCounts()
        {
            var outer = new InvalidOperationException("wrapper", caught(throwHere));
            Assert.That(CrashAttribution.Involvement(outer, a => a == tests), Is.Not.Null);
        }

        [Test]
        public void AnyAggregateInnerCounts()
        {
            var aggregate = new AggregateException(new InvalidOperationException("not thrown, no stack"), caught(throwHere));
            Assert.That(CrashAttribution.Involvement(aggregate, a => a == tests), Is.Not.Null);
        }

        [Test]
        public void ThrownFromAModsLoadContextIsOurs()
        {
            var context = new AssemblyLoadContext("test-mod", isCollectible: true);

            try
            {
                var semver = context.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, "Semver.dll"));
                var parse = semver.GetType("Semver.SemVersion", throwOnError: true)!.GetMethod("Parse", [typeof(string), typeof(int)])!;

                var thrown = caught(() => parse.Invoke(null, ["not a version", 1024]));

                Assert.That(CrashAttribution.Involvement(thrown), Is.EqualTo("Semver"));
            }
            finally
            {
                context.Unload();
            }
        }

        [Test]
        public void ThrownThroughAPatchedMethodIsOurs()
        {
            var harmony = new Harmony($"omamori.tests.{Guid.NewGuid()}");

            try
            {
                harmony.CreateClassProcessor(typeof(PatchThrower)).Patch();
                Assert.That(CrashAttribution.Involvement(caught(Thrower.Throw), _ => false), Is.EqualTo("Thrower.Throw, which is patched"));
            }
            finally
            {
                harmony.UnpatchAll(harmony.Id);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void throwHere() => throw new InvalidOperationException("from the tests");

        private static Exception caught(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                return e;
            }

            throw new AssertionException("expected a throw");
        }

        public static class Thrower
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Throw() => throw new InvalidOperationException("from a patched method");
        }

        [HarmonyPatch(typeof(Thrower), nameof(Thrower.Throw))]
        private static class PatchThrower
        {
            [HarmonyPrefix]
            private static void prefix()
            {
            }
        }
    }
}
