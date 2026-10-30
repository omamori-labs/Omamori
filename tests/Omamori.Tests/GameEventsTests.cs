using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core;
using osu.Game.Beatmaps;
using osu.Game.Scoring;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class GameEventsTests
    {
        [TearDown]
        public void TearDown()
        {
            GameEvents.Clear();
            FeatureRegistry.Set(Feature.ScreenEvents, FeatureState.Pending, null);
        }

        [Test]
        public void UnavailableHookIsSkippedNotThrown()
        {
            var log = Substitute.For<IModLogger>();
            bool called = false;

            new GameEvents(manifest(), log).OnScreenChanged(_ => called = true);
            GameEvents.Raise(GameEvent.ScreenChanged, new ScreenChange());

            Assert.That(called, Is.False);
            log.Received(1).Info(Arg.Is<string>(m => m.Contains("isn't available", StringComparison.Ordinal)));
        }

        [Test]
        public void ThrowingHandlerDoesNotStopTheRest()
        {
            FeatureRegistry.Set(Feature.ScreenEvents, FeatureState.Active, null);

            var events = new GameEvents(manifest(), Substitute.For<IModLogger>());
            ScreenChange? received = null;

            events.OnScreenChanged(_ => throw new InvalidOperationException("broken mod"));
            events.OnScreenChanged(change => received = change);

            var raised = new ScreenChange { Screen = GameScreen.SongSelect };
            Assert.DoesNotThrow(() => GameEvents.Raise(GameEvent.ScreenChanged, raised));
            Assert.That(received, Is.EqualTo(raised));
        }

        [Test]
        public void OnlyTheRaisedEventsHandlersRun()
        {
            FeatureRegistry.Set(Feature.ScreenEvents, FeatureState.Active, null);
            int calls = 0;

            new GameEvents(manifest(), Substitute.For<IModLogger>()).OnScreenChanged(_ => calls++);
            GameEvents.Raise(GameEvent.PlayEnded, new PlayResult());

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void NoScreenIsOther() => Assert.That(GameEventSources.Kind(null), Is.EqualTo(GameScreen.Other));

        [Test]
        public void BeatmapIsCopied()
        {
            var beatmap = new BeatmapInfo
            {
                DifficultyName = "Insane",
                StarRating = 5.5,
                BPM = 180,
                Length = 90_000,
                OnlineID = -1,
                Metadata = { Title = "song", Artist = "artist" },
            };

            var details = GameEventSources.Beatmap(beatmap);

            Assert.That(details.Title, Is.EqualTo("song"));
            Assert.That(details.Difficulty, Is.EqualTo("Insane"));
            Assert.That(details.Length, Is.EqualTo(TimeSpan.FromSeconds(90)));
            Assert.That(details.OnlineId, Is.Null, "local beatmaps have no online id");
        }

        [Test]
        public void ScoreIsCopied()
        {
            var details = GameEventSources.Score(new ScoreInfo { TotalScore = 1_000_000, Accuracy = 0.98, Rank = ScoreRank.S, MaxCombo = 500, Passed = true });

            Assert.That(details.TotalScore, Is.EqualTo(1_000_000));
            Assert.That(details.Rank, Is.EqualTo("S"));
            Assert.That(details.Pp, Is.Null);
        }

        [Test]
        public void StatisticsAccuracyIsZeroToOne()
        {
            var details = GameEventSources.Statistics(new osu.Game.Users.UserStatistics { Accuracy = 98.5, PP = 1234.5m, GlobalRank = 1000 });

            Assert.That(details.Accuracy, Is.EqualTo(0.985).Within(1e-9));
            Assert.That(details.Pp, Is.EqualTo(1234.5).Within(1e-9));
            Assert.That(details.GlobalRank, Is.EqualTo(1000));
        }

        [Test]
        public void HitsAreCopied()
        {
            var score = new ScoreInfo();
            score.HitEvents.Add(new osu.Game.Rulesets.Scoring.HitEvent(-12, null, osu.Game.Rulesets.Scoring.HitResult.Great, new osu.Game.Rulesets.Objects.HitObject { StartTime = 1500 }, null, new osuTK.Vector2(3, 4)));

            var hit = GameEventSources.Hits(score).Single();

            Assert.That(hit.Time, Is.EqualTo(1500));
            Assert.That(hit.Offset, Is.EqualTo(-12));
            Assert.That(hit.Result, Is.EqualTo("Great"));
            Assert.That(hit.X, Is.EqualTo(3));
        }

        [Test]
        public void ScoreWithoutBeatmapHasNone() => Assert.That(GameEventSources.Score(new ScoreInfo { BeatmapInfo = null }).Beatmap, Is.Null);

        private static ModManifest manifest() => new ModManifest
        {
            Id = "test.events",
            Hooks = new Dictionary<Hook, HookRequirement> { [Hook.ScreenEvents] = HookRequirement.Optional },
        };
    }
}
