using System;
using System.IO;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core;

namespace Omamori.Tests
{
    /// <summary>
    ///     each <see cref="FaultHistory.Open"/> is a new launch of the game
    /// </summary>
    [TestFixture]
    public class FaultHistoryTests
    {
        private static readonly ModManifest mod = new ModManifest { Id = "test.crashy", Version = "1.0.0" };

        private static readonly Exception crash = new InvalidOperationException("boom");

        private string path = null!;

        [SetUp]
        public void SetUp() => path = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}", FaultHistory.FILENAME);

        [TearDown]
        public void TearDown() => Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);

        [Test]
        public void NoHistoryLoads() => Assert.That(FaultHistory.Open(path).SkipReason(mod), Is.Null);

        [Test]
        public void OneCrashStillLoadsNextTime()
        {
            Assert.That(FaultHistory.Open(path).Record(mod, crash), Is.EqualTo(1));
            Assert.That(FaultHistory.Open(path).SkipReason(mod), Is.Null);
        }

        [Test]
        public void ManyCrashesInOneSessionAreOneStrike()
        {
            var session = FaultHistory.Open(path);
            session.Record(mod, crash);

            Assert.That(session.Record(mod, crash), Is.EqualTo(1));
        }

        [Test]
        public void TwoSessionsInARowSkip()
        {
            FaultHistory.Open(path).Record(mod, crash);
            Assert.That(FaultHistory.Open(path).Record(mod, crash), Is.EqualTo(FaultHistory.STRIKES_TO_SKIP));

            Assert.That(FaultHistory.Open(path).SkipReason(mod), Does.Contain("InvalidOperationException: boom"));
        }

        [Test]
        public void StaysSkippedWhileTheVersionIsTheSame()
        {
            FaultHistory.Open(path).Record(mod, crash);
            FaultHistory.Open(path).Record(mod, crash);
            FaultHistory.Open(path);

            Assert.That(FaultHistory.Open(path).SkipReason(mod), Is.Not.Null);
        }

        [Test]
        public void CleanSessionInBetweenStartsOver()
        {
            FaultHistory.Open(path).Record(mod, crash);
            FaultHistory.Open(path);

            Assert.That(FaultHistory.Open(path).Record(mod, crash), Is.EqualTo(1));
        }

        [Test]
        public void NewVersionClearsIt()
        {
            FaultHistory.Open(path).Record(mod, crash);
            FaultHistory.Open(path).Record(mod, crash);

            var updated = new ModManifest { Id = mod.Id, Version = "1.0.1" };
            Assert.That(FaultHistory.Open(path).SkipReason(updated), Is.Null);
            Assert.That(FaultHistory.Open(path).SkipReason(mod), Is.Null, "the old version's record is gone, not kept around");
        }

        [Test]
        public void OtherModsAreUnaffected()
        {
            FaultHistory.Open(path).Record(mod, crash);
            FaultHistory.Open(path).Record(mod, crash);

            Assert.That(FaultHistory.Open(path).SkipReason(new ModManifest { Id = "test.fine", Version = "1.0.0" }), Is.Null);
        }

        [Test]
        public void UnreadableFileStartsOver()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{ not json");

            FaultHistory? history = null;
            Assert.DoesNotThrow(() => history = FaultHistory.Open(path));
            Assert.That(history!.Session, Is.EqualTo(1));
        }
    }
}
