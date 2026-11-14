using System;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Plays;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;

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

        [Test]
        public void LateStartIsNeverSubmitted()
        {
            var starting = new PlayStarting();

            starting.StartAt(TimeSpan.FromSeconds(30));

            Assert.That(starting.SuppressionReason, Is.Not.Null);
        }

        [Test]
        public void FirstStartTimeIsKept()
        {
            var starting = new PlayStarting();

            starting.StartAt(TimeSpan.FromSeconds(30));
            starting.StartAt(TimeSpan.FromSeconds(60));

            Assert.That(starting.StartTime, Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        public void StartingAtTheBeginningChangesNothing()
        {
            var starting = new PlayStarting();

            starting.StartAt(TimeSpan.Zero);

            Assert.That(starting.StartTime, Is.Null);
            Assert.That(starting.SuppressionReason, Is.Null);
        }

        [Test]
        public void LateStartLeavesOutEarlierObjects()
        {
            var beatmap = beatmapWithObjectsAt(1000, 2000, 3000);

            Assert.That(LateStart.Trim(beatmap, 1500), Is.True);
            Assert.That(beatmap.HitObjects.Select(h => h.StartTime), Is.EqualTo([2000, 3000]));
        }

        [Test]
        public void LateStartPastTheLastObjectKeepsTheWholeMap()
        {
            var beatmap = beatmapWithObjectsAt(1000, 2000);

            Assert.That(LateStart.Trim(beatmap, 5000), Is.False);
            Assert.That(beatmap.HitObjects, Has.Count.EqualTo(2));
        }

        [Test]
        public void EveryRetryStartsLate()
        {
            gamePlays("test.plays").OnStarting(starting => starting.StartAt(TimeSpan.FromSeconds(1.5)));
            GamePlays.Begin();

            foreach (object retry in new[] { new object(), new object() })
            {
                GamePlays.Tag(retry, GamePlays.Current!);
                GamePlays.PrepareStart(retry, beatmapWithObjectsAt(1000, 2000));

                Assert.That(GamePlays.StartTimeFor(retry), Is.EqualTo(1500));
            }
        }

        [Test]
        public void PlayerWithNothingLeftStartsFromTheBeginning()
        {
            gamePlays("test.plays").OnStarting(starting => starting.StartAt(TimeSpan.FromSeconds(10)));
            GamePlays.Begin();

            object player = new object();
            GamePlays.Tag(player, GamePlays.Current!);
            GamePlays.PrepareStart(player, beatmapWithObjectsAt(1000, 2000));

            Assert.That(GamePlays.StartTimeFor(player), Is.Null);
        }

        private static Beatmap<HitObject> beatmapWithObjectsAt(params double[] times) => new Beatmap<HitObject>
        {
            HitObjects = [.. times.Select(t => new HitObject { StartTime = t })],
        };

        private static GamePlays gamePlays(string id) => new GamePlays(new ModManifest { Id = id }, Substitute.For<IModLogger>());
    }
}
