using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Events.Sources;

namespace Omamori.Tests
{
    /// <summary>
    ///     driven the way realm reports changes, deletions by old index, insertions and modifications by new index
    /// </summary>
    [TestFixture]
    public class BeatmapSetMirrorTests
    {
        [Test]
        public void RemovedSetsAreDescribedFromTheMirror()
        {
            var mirror = mirrorOf("a", "b", "c");

            var removed = mirror.Remove([1]);

            Assert.That(titles(removed), Is.EqualTo("b"));
            Assert.That(mirror.Count, Is.EqualTo(2));
        }

        // removing from the front first would shift the later index onto the wrong set
        [Test]
        public void SeveralDeletionsUseTheOldIndices()
        {
            var mirror = mirrorOf("a", "b", "c", "d");

            var removed = mirror.Remove([0, 2]);

            Assert.That(titles(removed), Is.EqualTo("a, c"));
            mirror.Remove([0]);
            Assert.That(titles(mirror.Remove([0])), Is.EqualTo("d"));
        }

        [Test]
        public void InsertionsLandAtTheirNewIndices()
        {
            var mirror = mirrorOf("a", "c");

            mirror.Insert([(3, set("d")), (1, set("b"))]);

            Assert.That(titles(mirror.Remove([0, 1, 2, 3])), Is.EqualTo("a, b, c, d"));
        }

        // a delete and an insert in one change set, like a set being reimported
        [Test]
        public void DeletionsApplyBeforeInsertions()
        {
            var mirror = mirrorOf("a", "b", "c");

            Assert.That(titles(mirror.Remove([1])), Is.EqualTo("b"));
            mirror.Insert([(1, set("b2"))]);

            Assert.That(titles(mirror.Remove([0, 1, 2])), Is.EqualTo("a, b2, c"));
        }

        [Test]
        public void ModifiedSetsAreKeptCurrent()
        {
            var mirror = mirrorOf("a", "b");

            mirror.Replace([(1, set("b renamed"))]);

            Assert.That(titles(mirror.Remove([1])), Is.EqualTo("b renamed"));
        }

        [Test]
        public void ResetReplacesEverything()
        {
            var mirror = mirrorOf("a", "b");

            mirror.Reset([set("x")]);

            Assert.That(titles(mirror.Remove([0])), Is.EqualTo("x"));
        }

        private static BeatmapSetMirror mirrorOf(params string[] titles)
        {
            var mirror = new BeatmapSetMirror();
            mirror.Reset(titles.Select(set));
            return mirror;
        }

        private static BeatmapSetDetails set(string title) => new BeatmapSetDetails { Title = title };

        private static string titles(IEnumerable<BeatmapSetDetails> sets) => string.Join(", ", sets.Select(s => s.Title));
    }
}
