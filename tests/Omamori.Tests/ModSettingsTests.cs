using System;
using System.IO;
using NUnit.Framework;
using Omamori.Core;

namespace Omamori.Tests
{
    /// <summary>
    ///     each <see cref="ModSettings.Open"/> is a new launch of the game
    /// </summary>
    [TestFixture]
    public class ModSettingsTests
    {
        private string path = null!;

        [SetUp]
        public void SetUp() => path = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}", ModSettings.FILENAME);

        [TearDown]
        public void TearDown()
        {
            string directory = Path.GetDirectoryName(path)!;

            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        [Test]
        public void EveryModStartsOn() => Assert.That(ModSettings.Open(path).IsEnabled("test.mod"), Is.True);

        [Test]
        public void TurnedOffStaysOffNextLaunch()
        {
            ModSettings.Open(path).SetEnabled("test.mod", false);

            var next = ModSettings.Open(path);
            Assert.That(next.IsEnabled("test.mod"), Is.False);
            Assert.That(next.IsEnabled("test.other"), Is.True);
        }

        [Test]
        public void TurningBackOnSticks()
        {
            var settings = ModSettings.Open(path);
            settings.SetEnabled("test.mod", false);
            settings.SetEnabled("test.mod", false);
            settings.SetEnabled("test.mod", true);

            Assert.That(ModSettings.Open(path).IsEnabled("test.mod"), Is.True);
        }

        [Test]
        public void NothingIsWrittenUntilSomethingChanges()
        {
            ModSettings.Open(path).SetEnabled("test.mod", true);
            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public void UnreadableFileTurnsNothingOff()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{ not json");

            ModSettings? settings = null;
            Assert.DoesNotThrow(() => settings = ModSettings.Open(path));
            Assert.That(settings!.IsEnabled("test.mod"), Is.True);
        }
    }
}
