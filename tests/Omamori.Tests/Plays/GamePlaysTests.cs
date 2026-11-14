using System;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Plays;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class GamePlaysTests
    {
        [SetUp]
        public void SetUp() => FeatureRegistry.Set(Feature.PlayStarting, FeatureState.Active, null);

        [TearDown]
        public void TearDown() => CoreState.Reset();

        [Test]
        public void PlayStartsWhenNobodyIsListening() => Assert.That(GamePlays.Begin(), Is.True);

        [Test]
        public void HandlerCanCancelThePlay()
        {
            gamePlays("test.plays").OnStarting(starting => starting.Cancel());

            Assert.That(GamePlays.Begin(), Is.False);
            Assert.That(GamePlays.Current, Is.Null);
        }

        [Test]
        public void SuppressedPlayIsRememberedForItsPlayer()
        {
            gamePlays("test.plays").OnStarting(starting => starting.SuppressSubmission("custom start"));

            Assert.That(GamePlays.Begin(), Is.True);

            object player = new object();
            GamePlays.Tag(player, GamePlays.Current!);

            Assert.That(GamePlays.Unsubmitted(player), Is.EqualTo("custom start"));
            Assert.That(GamePlays.Unsubmitted(new object()), Is.Null);
        }

        [Test]
        public void FirstSuppressionReasonIsKept()
        {
            var starting = new PlayStarting();

            starting.SuppressSubmission("first");
            starting.SuppressSubmission("second");

            Assert.That(starting.SuppressionReason, Is.EqualTo("first"));
        }

        [Test]
        public void EndingForgetsTheCurrentPlay()
        {
            gamePlays("test.plays").OnStarting(starting => starting.SuppressSubmission("custom start"));

            GamePlays.Begin();
            GamePlays.End();

            Assert.That(GamePlays.Current, Is.Null);
        }

        [Test]
        public void ThrowingHandlerDoesNotStopTheRest()
        {
            var plays = gamePlays("test.plays");
            plays.OnStarting(_ => throw new InvalidOperationException("broken mod"));
            plays.OnStarting(starting => starting.Cancel());

            Assert.That(GamePlays.Begin(), Is.False);
        }

        [Test]
        public void UnavailableHookIsSkippedNotThrown()
        {
            FeatureRegistry.Set(Feature.PlayStarting, FeatureState.Failed, "test");

            Assert.DoesNotThrow(() => gamePlays("test.plays").OnStarting(starting => starting.Cancel()));
            Assert.That(GamePlays.Begin(), Is.True);
        }

        [Test]
        public void BrokenCheckKeepsPlaysUnsubmittedWhileModsListen()
        {
            gamePlays("test.plays").OnStarting(_ => { });
            FeatureRegistry.Set(Feature.PlayStarting, FeatureState.Faulted, "test");

            Assert.That(GamePlays.Unsubmitted(new object()), Is.Not.Null);
        }

        [Test]
        public void BrokenCheckSubmitsWhenNoModListens()
        {
            FeatureRegistry.Set(Feature.PlayStarting, FeatureState.Faulted, "test");

            Assert.That(GamePlays.Unsubmitted(new object()), Is.Null);
        }

        private static GamePlays gamePlays(string id) => new GamePlays(new ModManifest { Id = id }, Substitute.For<IModLogger>());
    }
}
