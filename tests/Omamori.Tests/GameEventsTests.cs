using System;
using System.Collections.Generic;
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

        private static ModManifest manifest() => new ModManifest
        {
            Id = "test.events",
            Hooks = new Dictionary<Hook, HookRequirement> { [Hook.ScreenEvents] = HookRequirement.Optional },
        };
    }
}
