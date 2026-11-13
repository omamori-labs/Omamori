using System;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Mods;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class GameModsTests
    {
        [SetUp]
        public void SetUp() => FeatureRegistry.Set(Feature.GameMods, FeatureState.Active, null);

        [TearDown]
        public void TearDown() => CoreState.Reset();

        [Test]
        public void ModWithoutRulesetsShowsInEveryRuleset()
        {
            gameMods("test.mods").Add<TestMod>();

            Assert.That(GameMods.For(ruleset("osu")), Has.Count.EqualTo(1));
            Assert.That(GameMods.For(ruleset("mania")), Has.Count.EqualTo(1));
        }

        [Test]
        public void ModOnlyShowsInTheRulesetsItAskedFor()
        {
            gameMods("test.mods").Add<TestMod>("osu", "taiko");

            Assert.That(GameMods.For(ruleset("osu")), Has.Count.EqualTo(1));
            Assert.That(GameMods.For(ruleset("TAIKO")), Has.Count.EqualTo(1));
            Assert.That(GameMods.For(ruleset("mania")), Is.Empty);
        }

        [Test]
        public void EveryListingGetsItsOwnInstance()
        {
            gameMods("test.mods").Add<TestMod>();

            var first = GameMods.For(ruleset("osu"))[0];
            var second = GameMods.For(ruleset("osu"))[0];

            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void FactoryCanHandEveryInstanceTheSameState()
        {
            object shared = new object();
            var seen = new System.Collections.Generic.List<object>();

            gameMods("test.mods").Add(() =>
            {
                seen.Add(shared);
                return new TestMod();
            });

            GameMods.For(ruleset("osu"));
            GameMods.For(ruleset("mania"));

            Assert.That(seen, Is.EqualTo([shared, shared]));
        }

        [Test]
        public void HostRulesetNeverGetsMods()
        {
            gameMods("test.mods").Add<TestMod>();

            Assert.That(GameMods.For(ruleset(Omamori.Core.CoreEntry.HostRulesetShortName)), Is.Empty);
        }

        [Test]
        public void UnavailableHookIsSkippedNotThrown()
        {
            FeatureRegistry.Set(Feature.GameMods, FeatureState.Failed, "test");

            Assert.DoesNotThrow(() => gameMods("test.mods").Add<TestMod>());
            Assert.That(GameMods.For(ruleset("osu")), Is.Empty);
        }

        [Test]
        public void ThrowingModDoesNotStopTheRest()
        {
            var mods = gameMods("test.mods");
            mods.Add<ThrowingMod>();
            mods.Add<TestMod>();

            Assert.That(GameMods.For(ruleset("osu")), Has.Count.EqualTo(1));
        }

        private static GameMods gameMods(string id) => new GameMods(new ModManifest { Id = id }, Substitute.For<IModLogger>());

        private static Ruleset ruleset(string shortName)
        {
            var ruleset = Substitute.For<Ruleset>();
            ruleset.ShortName.Returns(shortName);
            return ruleset;
        }

        private class TestMod : Mod
        {
            public override string Name => "Test";
            public override string Acronym => "TS";
            public override osu.Framework.Localisation.LocalisableString Description => "test";
            public override ModType Type => ModType.Fun;
        }

        private sealed class ThrowingMod : TestMod
        {
            public ThrowingMod() => throw new InvalidOperationException("broken mod");
        }
    }
}
