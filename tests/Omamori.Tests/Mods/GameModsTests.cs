using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core;
using Omamori.Core.Features;
using Omamori.Core.Mods;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace Omamori.Tests
{
    // no test type may derive from a game type, nunit can't load it before the game assemblies resolve and then finds no tests at all
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
            gameMods("test.mods").Add<ModNoMod>();

            Assert.That(GameMods.For(ruleset("osu")), Has.Count.EqualTo(1));
            Assert.That(GameMods.For(ruleset("mania")), Has.Count.EqualTo(1));
        }

        [Test]
        public void ModOnlyShowsInTheRulesetsItAskedFor()
        {
            gameMods("test.mods").Add<ModNoMod>("osu", "taiko");

            Assert.That(GameMods.For(ruleset("osu")), Has.Count.EqualTo(1));
            Assert.That(GameMods.For(ruleset("TAIKO")), Has.Count.EqualTo(1));
            Assert.That(GameMods.For(ruleset("mania")), Is.Empty);
        }

        [Test]
        public void EveryListingGetsItsOwnInstance()
        {
            gameMods("test.mods").Add<ModNoMod>();

            var first = GameMods.For(ruleset("osu"))[0];
            var second = GameMods.For(ruleset("osu"))[0];

            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void FactoryCanHandEveryInstanceTheSameState()
        {
            object shared = new object();
            var seen = new List<object>();

            gameMods("test.mods").Add(() =>
            {
                seen.Add(shared);
                return new ModNoMod();
            });

            GameMods.For(ruleset("osu"));
            GameMods.For(ruleset("mania"));

            Assert.That(seen, Is.EqualTo([shared, shared]));
        }

        [Test]
        public void HostRulesetNeverGetsMods()
        {
            gameMods("test.mods").Add<ModNoMod>();

            Assert.That(GameMods.For(ruleset(CoreEntry.HostRulesetShortName)), Is.Empty);
        }

        [Test]
        public void UnavailableHookIsSkippedNotThrown()
        {
            FeatureRegistry.Set(Feature.GameMods, FeatureState.Failed, "test");

            Assert.DoesNotThrow(() => gameMods("test.mods").Add<ModNoMod>());
            Assert.That(GameMods.For(ruleset("osu")), Is.Empty);
        }

        [Test]
        public void ThrowingModDoesNotStopOtherMods()
        {
            gameMods("test.broken").Add(() => throw new InvalidOperationException("broken mod"));
            gameMods("test.working").Add<ModNoMod>();

            Assert.That(GameMods.For(ruleset("osu")), Has.Count.EqualTo(1));
        }

        [Test]
        public void ModSelectGetsTheModUnderItsType()
        {
            gameMods("test.mods").Add<ModNoMod>();

            var lists = GameMods.AddTo([], ruleset("osu"));

            Assert.That(lists[new ModNoMod().Type], Has.One.InstanceOf<ModNoMod>());
        }

        [Test]
        public void ModSelectNeverListsAModTwice()
        {
            gameMods("test.mods").Add<ModNoMod>();

            var once = GameMods.AddTo([], ruleset("osu"));
            var twice = GameMods.AddTo(once, ruleset("osu"));

            Assert.That(twice[new ModNoMod().Type], Has.Count.EqualTo(1));
        }

        [Test]
        public void ModSelectIsUntouchedWithNothingToAdd()
        {
            var available = new Dictionary<ModType, IReadOnlyList<Mod>>();

            Assert.That(GameMods.AddTo(available, ruleset("osu")), Is.SameAs(available));
        }

        [Test]
        public void PlayWithAModsModIsNotSubmitted()
        {
            gameMods("test.mods").Add<ModNoMod>();
            var mods = GameMods.For(ruleset("osu"));

            Assert.That(GameMods.Unsubmitted(mods), Is.Not.Null);
        }

        [Test]
        public void PlayWithOnlyLazerModsIsSubmitted()
        {
            gameMods("test.mods").Add<ModNoMod>();

            Assert.That(GameMods.Unsubmitted([Substitute.For<Mod>()]), Is.Null);
        }

        [Test]
        public void BrokenCheckKeepsPlaysUnsubmittedWhileModsAddMods()
        {
            gameMods("test.mods").Add<ModNoMod>();
            FeatureRegistry.Set(Feature.GameMods, FeatureState.Faulted, "test");

            Assert.That(GameMods.Unsubmitted([]), Is.Not.Null);
        }

        [Test]
        public void BrokenCheckSubmitsWhenNoModAddsMods()
        {
            FeatureRegistry.Set(Feature.GameMods, FeatureState.Faulted, "test");

            Assert.That(GameMods.Unsubmitted([]), Is.Null);
        }

        private static GameMods gameMods(string id) => new GameMods(new ModManifest { Id = id }, Substitute.For<IModLogger>());

        private static Ruleset ruleset(string shortName)
        {
            var ruleset = Substitute.For<Ruleset>();
            ruleset.ShortName.Returns(shortName);
            return ruleset;
        }
    }
}
