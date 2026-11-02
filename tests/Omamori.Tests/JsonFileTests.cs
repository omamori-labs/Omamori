using System;
using System.IO;
using NUnit.Framework;
using Omamori.Core;

namespace Omamori.Tests
{
    [TestFixture]
    public class JsonFileTests
    {
        private string directory = null!;
        private string path = null!;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}");
            path = Path.Combine(directory, "state", "state.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        [Test]
        public void MissingFileStartsFresh() => Assert.That(JsonFile.Read<State>(path, "starting fresh").Name, Is.Null);

        [Test]
        public void UnreadableFileStartsFresh()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{ not json");

            State? state = null;
            Assert.DoesNotThrow(() => state = JsonFile.Read<State>(path, "starting fresh"));
            Assert.That(state!.Name, Is.Null);
        }

        [Test]
        public void SavedValueReadsBackAndLeavesNoTemporaryFile()
        {
            JsonFile.Write(path, new State { Name = "saved" });

            Assert.That(JsonFile.Read<State>(path, "starting fresh").Name, Is.EqualTo("saved"));
            Assert.That(File.Exists(path + ".tmp"), Is.False);
        }

        [Test]
        public void FailedSaveKeepsTheOldFile()
        {
            JsonFile.Write(path, new State { Name = "old" });
            Directory.CreateDirectory(path + ".tmp");

            Assert.DoesNotThrow(() => JsonFile.Write(path, new State { Name = "new" }));
            Assert.That(JsonFile.Read<State>(path, "starting fresh").Name, Is.EqualTo("old"));
        }

        internal sealed class State
        {
            public string? Name { get; set; }
        }
    }
}
