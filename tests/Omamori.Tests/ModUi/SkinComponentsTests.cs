using System;
using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.ModUi;
using Omamori.Core.Safety;
using osu.Game.Skinning.Components;

namespace Omamori.Tests
{
    // lazer's own BoxElement stands in for a mod's component, a test type deriving from a game type would stop nunit finding any tests
    [TestFixture]
    [NonParallelizable]
    public class SkinComponentsTests
    {
        private static readonly string box = typeof(BoxElement).AssemblyQualifiedName!;
        private const string missing = "Gone.Component, Gone.Mod, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        [SetUp]
        public void SetUp() => FeatureRegistry.Set(Feature.SkinComponents, FeatureState.Active, null);

        [TearDown]
        public void TearDown() => CoreState.Reset();

        [Test]
        public void LayoutWithNothingMissingIsTheSameString()
        {
            string json = layout($"{{\"type\":\"{box}\"}}");

            Assert.That(SkinComponents.Filter(json, out int leftOut), Is.SameAs(json));
            Assert.That(leftOut, Is.Zero);
        }

        [Test]
        public void ComponentOfAModThatIsntLoadedIsLeftOutAndTheRestStays()
        {
            string filtered = SkinComponents.Filter(layout($"{{\"type\":\"{missing}\"}}", $"{{\"type\":\"{box}\"}}"), out int leftOut);

            Assert.That(leftOut, Is.EqualTo(1));
            Assert.That(filtered, Does.Not.Contain("Gone.Component"));
            Assert.That(filtered, Does.Contain(nameof(BoxElement)));
        }

        [Test]
        public void MissingComponentInsideAnotherIsLeftOut()
        {
            string filtered = SkinComponents.Filter(layout($"{{\"type\":\"{box}\",\"children\":[{{\"Type\":\"{missing}\"}}]}}"), out int leftOut);

            Assert.That(leftOut, Is.EqualTo(1));
            Assert.That(filtered, Does.Contain(nameof(BoxElement)));
        }

        [Test]
        public void UnknownTypeInAnAssemblyThatLoadsIsLeftForLazer()
        {
            string json = layout($"{{\"type\":\"osu.Game.Skinning.Components.NotAThing, {typeof(BoxElement).Assembly.GetName().Name}\"}}");

            Assert.That(SkinComponents.Filter(json, out _), Is.SameAs(json));
        }

        [Test]
        public void FaultedModsComponentIsLeftOut()
        {
            hooks("test.skin").AddSkinComponent<BoxElement>();
            ModFaults.TurnOff("test.skin");

            SkinComponents.Filter(layout($"{{\"type\":\"{box}\"}}"), out int leftOut);

            Assert.That(leftOut, Is.EqualTo(1));
        }

        [Test]
        public void ToolboxListsRunningModsComponentsOnce()
        {
            hooks("test.skin").AddSkinComponent<BoxElement>();

            Assert.That(SkinComponents.WithModComponents([typeof(string)]), Is.EqualTo((Type[])[typeof(string), typeof(BoxElement)]));
            Assert.That(SkinComponents.WithModComponents([typeof(BoxElement)]), Has.Length.EqualTo(1));
        }

        [Test]
        public void ToolboxLeavesOutAFaultedModsComponents()
        {
            hooks("test.skin").AddSkinComponent<BoxElement>();
            ModFaults.TurnOff("test.skin");

            Assert.That(SkinComponents.WithModComponents([]), Is.Empty);
        }

        [Test]
        public void ModAssemblyIsFoundByNameWhateverItsVersion()
        {
            hooks("test.skin").AddSkinComponent<BoxElement>();
            var name = new AssemblyName(typeof(BoxElement).Assembly.GetName().Name!) { Version = new Version(99, 0) };

            Assert.That(SkinComponents.Resolve(name), Is.SameAs(typeof(BoxElement).Assembly));
            Assert.That(SkinComponents.Resolve(new AssemblyName("Gone.Mod")), Is.Null);
        }

        [Test]
        public void UnavailableHookSkipsTheComponent()
        {
            FeatureRegistry.Set(Feature.SkinComponents, FeatureState.Failed, "test");

            hooks("test.skin").AddSkinComponent<BoxElement>();

            Assert.That(SkinComponents.WithModComponents([]), Is.Empty);
        }

        [Test]
        public void BrokenJsonIsLeftForLazer() => Assert.That(SkinComponents.Filter("{not json", out _), Is.EqualTo("{not json"));

        // the shape lazer saves, drawables per ruleset key
        private static string layout(params string[] drawables) => $"{{\"version\":3,\"drawableInfo\":{{\"global\":[{string.Join(",", drawables)}]}}}}";

        private static UiHooks hooks(string id) => new UiHooks(new ModManifest { Id = id }, Substitute.For<IModLogger>());
    }
}
