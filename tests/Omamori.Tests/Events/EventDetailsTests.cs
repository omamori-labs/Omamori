using System;
using System.Linq;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core;
using osu.Game.Beatmaps;
using osu.Game.Scoring;

namespace Omamori.Tests
{
    /// <summary>
    ///     the read-only copies mods get from lazer's objects
    /// </summary>
    [TestFixture]
    public class EventDetailsTests
    {
        [Test]
        public void NoScreenIsOther() => Assert.That(EventDetails.Screen(null), Is.EqualTo(GameScreen.Other));

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

            var details = EventDetails.Beatmap(beatmap);

            Assert.That(details.Title, Is.EqualTo("song"));
            Assert.That(details.Difficulty, Is.EqualTo("Insane"));
            Assert.That(details.Length, Is.EqualTo(TimeSpan.FromSeconds(90)));
            Assert.That(details.OnlineId, Is.Null, "local beatmaps have no online id");
        }

        [Test]
        public void ScoreIsCopied()
        {
            var details = EventDetails.Score(new ScoreInfo { TotalScore = 1_000_000, Accuracy = 0.98, Rank = ScoreRank.S, MaxCombo = 500, Passed = true });

            Assert.That(details.TotalScore, Is.EqualTo(1_000_000));
            Assert.That(details.Rank, Is.EqualTo("S"));
            Assert.That(details.Pp, Is.Null);
        }

        [Test]
        public void StatisticsAccuracyIsZeroToOne()
        {
            var details = EventDetails.Statistics(new osu.Game.Users.UserStatistics { Accuracy = 98.5, PP = 1234.5m, GlobalRank = 1000 });

            Assert.That(details.Accuracy, Is.EqualTo(0.985).Within(1e-9));
            Assert.That(details.Pp, Is.EqualTo(1234.5).Within(1e-9));
            Assert.That(details.GlobalRank, Is.EqualTo(1000));
        }

        [Test]
        public void HitsAreCopied()
        {
            var score = new ScoreInfo();
            score.HitEvents.Add(new osu.Game.Rulesets.Scoring.HitEvent(-12, null, osu.Game.Rulesets.Scoring.HitResult.Great, new osu.Game.Rulesets.Objects.HitObject { StartTime = 1500 }, null, new osuTK.Vector2(3, 4)));

            var hit = EventDetails.Hits(score).Single();

            Assert.That(hit.Time, Is.EqualTo(1500));
            Assert.That(hit.Offset, Is.EqualTo(-12));
            Assert.That(hit.Result, Is.EqualTo("Great"));
            Assert.That(hit.X, Is.EqualTo(3));
        }

        [Test]
        public void ScoreWithoutBeatmapHasNone() => Assert.That(EventDetails.Score(new ScoreInfo { BeatmapInfo = null }).Beatmap, Is.Null);
    }
}
