using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core;
using static VerifyNUnit.Verifier;

namespace Omamori.Tests
{
    [TestFixture]
    public class DependencyResolutionTests
    {
        [Test]
        public void DependencyLoadsBeforeDependentWhateverTheFolderOrder()
        {
            var (order, skipped) = resolve(mod("a.dependent", requires("z.base")), mod("z.base"));

            Assert.That(skipped, Is.Empty);
            string[] expected = ["z.base", "a.dependent"];
            Assert.That(order, Is.EqualTo(expected));
        }

        [Test]
        public void MissingRequiredDependencySkipsTheMod()
        {
            var (order, skipped) = resolve(mod("a.mod", requires("not.installed")));

            Assert.That(order, Is.Empty);
            Assert.That(skipped["a.mod"], Is.EqualTo("needs not.installed, which isn't installed or can't load"));
        }

        [Test]
        public void TooOldDependencySkipsTheMod()
        {
            var (_, skipped) = resolve(mod("base", version: "1.0.0"), mod("dependent", requires("base", "2.0")));
            Assert.That(skipped["dependent"], Is.EqualTo("needs base 2.0 or newer, 1.0.0 is installed"));
        }

        [Test]
        public void PrereleaseDoesNotSatisfyItsRelease()
        {
            var (_, skipped) = resolve(mod("base", version: "1.0.0-beta"), mod("dependent", requires("base", "1.0.0")));
            Assert.That(skipped, Does.ContainKey("dependent"));
        }

        [Test]
        public void FailureCarriesDownTheChain()
        {
            var (order, skipped) = resolve(mod("a", requires("b")), mod("b", requires("c")), mod("c", requires("missing")));

            Assert.That(order, Is.Empty);
            Assert.That(skipped["a"], Is.EqualTo("needs b, which was skipped"));
            Assert.That(skipped["b"], Is.EqualTo("needs c, which was skipped"));
        }

        [Test]
        public void OnlyModsInACycleAreReportedAsTheCycle()
        {
            var (order, skipped) = resolve(mod("cycle.a", requires("cycle.b")), mod("cycle.b", requires("cycle.a")), mod("on.cycle", requires("cycle.a")), mod("free"));

            string[] expected = ["free"];
            Assert.That(order, Is.EqualTo(expected));
            Assert.That(skipped["cycle.a"], Does.StartWith("dependency cycle"));
            Assert.That(skipped["cycle.b"], Does.StartWith("dependency cycle"));
            Assert.That(skipped["on.cycle"], Is.EqualTo("needs cycle.a, which was skipped"));
        }

        [Test]
        public void MissingOptionalDependencyStillLoads()
        {
            var (order, skipped) = resolve(mod("a", optional("not.installed")));

            Assert.That(skipped, Is.Empty);
            string[] expected = ["a"];
            Assert.That(order, Is.EqualTo(expected));
        }

        [Test]
        public void PresentOptionalDependencyLoadsFirst()
        {
            var (order, _) = resolve(mod("a", optional("z")), mod("z"));
            string[] expected = ["z", "a"];
            Assert.That(order, Is.EqualTo(expected));
        }

        [Test]
        public void OptionalLoopKeepsBothMods()
        {
            var (order, skipped) = resolve(mod("loop.a", optional("loop.b")), mod("loop.b", requires("loop.a")));

            Assert.That(skipped, Is.Empty);
            string[] expected = ["loop.a", "loop.b"];
            Assert.That(order, Is.EqualTo(expected));
        }

        [Test]
        public void OrderIsTheSameEveryRun()
        {
            ModCandidate[] mods = [mod("e", requires("a")), mod("d"), mod("c", requires("e")), mod("b"), mod("a")];
            var first = resolve(mods).Order;

            for (int i = 0; i < 5; i++)
                Assert.That(resolve(mods).Order, Is.EqualTo(first));
        }

        /// <summary>everything at once, the skip reasons are what users read</summary>
        [Test]
        public Task MixedScenario()
        {
            var (order, skipped) = resolve(
                mod("a.dependent", requires("test.menu", "0.1.0"), optional("not.installed")),
                mod("chain", requires("cycle.a")),
                mod("cycle.a", requires("cycle.b")),
                mod("cycle.b", requires("cycle.a")),
                mod("test.menu", version: "0.1.0"),
                mod("too.old", requires("test.menu", "9.0")),
                mod("prealpha", requires("test.menu", "0.1.1-alpha")),
                mod("prebeta", requires("test.menu", "0.1.0-beta")));

            return Verify(new { order, skipped });
        }

        private static (List<string> Order, Dictionary<string, string> Skipped) resolve(params ModCandidate[] candidates)
        {
            var skipped = new List<SkippedMod>();
            var order = ModLoader.ResolveDependencies([.. candidates], skipped);

            return ([.. order.Select(c => c.Manifest.Id)], skipped.ToDictionary(s => s.Name, s => s.Reason));
        }

        // name is the id so skip reasons are keyed by id in assertions
        private static ModCandidate mod(string id, params ModDependency[] dependencies) => mod(id, "1.0.0", dependencies);

        private static ModCandidate mod(string id, string version, params ModDependency[] dependencies) =>
            new ModCandidate($"mods/{id}", new ModManifest { Id = id, Name = id, Version = version, Dependencies = [.. dependencies] });

        private static ModDependency requires(string id, string minVersion = "") => new ModDependency { Id = id, MinVersion = minVersion };

        private static ModDependency optional(string id) => new ModDependency { Id = id, Optional = true };
    }
}
