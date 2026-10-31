using System.Collections.Generic;
using NUnit.Framework;
using Omamori.Core.Events.Sources;

namespace Omamori.Tests
{
    [TestFixture]
    public class HitCursorTests
    {
        [Test]
        public void OnlyNewHitsAreUnseen()
        {
            var cursor = new HitCursor();
            List<int> hits = [1, 2];

            Assert.That(cursor.Unseen(hits), Is.EqualTo(hits));

            hits.Add(3);
            Assert.That(cursor.Unseen(hits), Is.EqualTo(new List<int> { 3 }));
            Assert.That(cursor.Unseen(hits), Is.Empty);
        }

        [Test]
        public void HitsAfterASeekBackAreSeenAgain()
        {
            var cursor = new HitCursor();
            List<int> hits = [1, 2, 3];
            cursor.Unseen(hits);

            hits.RemoveRange(1, 2);
            cursor.Rewind(hits.Count);
            hits.Add(5);

            Assert.That(cursor.Unseen(hits), Is.EqualTo(new List<int> { 5 }));
        }

        [Test]
        public void AShorterListWithoutARewindIsNotReplayed()
        {
            var cursor = new HitCursor();
            List<int> hits = [1, 2, 3];
            cursor.Unseen(hits);

            hits.RemoveRange(1, 2);
            Assert.That(cursor.Unseen(hits), Is.Empty);

            hits.Add(4);
            Assert.That(cursor.Unseen(hits), Is.EqualTo(new List<int> { 4 }));
        }
    }
}
