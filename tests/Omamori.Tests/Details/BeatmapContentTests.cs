using System.Linq;
using NUnit.Framework;
using Omamori.Core.Details;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;

namespace Omamori.Tests
{
    [TestFixture]
    public class BeatmapContentTests
    {
        [Test]
        public void ObjectsTimingAndBreaksAreCopiedInOrder()
        {
            var beatmap = new Beatmap<HitObject>
            {
                HitObjects = [new HitObject { StartTime = 1000 }, new HitObject { StartTime = 2000 }],
                Breaks = [new BreakPeriod(1200, 1800)],
            };
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            var content = GameDetails.Content(beatmap, new BeatmapInfo(), new RulesetInfo { ShortName = "osu" }, []);

            Assert.That(content.HitObjects.Select(h => h.StartTime), Is.EqualTo([1000, 2000]));
            Assert.That(content.TimingPoints.Single().Bpm, Is.EqualTo(120));
            Assert.That(content.TimingPoints.Single().BeatsPerBar, Is.EqualTo(4));
            Assert.That(content.Breaks.Single().EndTime, Is.EqualTo(1800));
            Assert.That(content.Ruleset, Is.EqualTo("osu"));
        }

        [Test]
        public void ObjectWithoutALengthEndsWhereItStarts()
        {
            var details = GameDetails.HitObjectOf(new HitObject { StartTime = 750 });

            Assert.That(details.EndTime, Is.EqualTo(750));
            Assert.That(details.Type, Is.EqualTo(nameof(HitObject)));
        }

        [Test]
        public void ObjectWithoutAPositionHasNone()
        {
            var details = GameDetails.HitObjectOf(new HitObject());

            Assert.That(details.X, Is.Null);
            Assert.That(details.Y, Is.Null);
            Assert.That(details.IsNewCombo, Is.False);
        }
    }
}
