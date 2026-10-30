using System;
using System.IO;
using NUnit.Framework;
using Omamori.Core;

namespace Omamori.Tests
{
    /// <summary>
    ///     each <see cref="ModSwitches.Open"/> is a new launch of the game
    /// </summary>
    [TestFixture]
    public class ModSwitchesTests
    {
        private string path = null!;

        [SetUp]
        public void SetUp() => path = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}", ModSwitches.FILENAME);

        [TearDown]
        public void TearDown()
        {
            string directory = Path.GetDirectoryName(path)!;

            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        [Test]
        public void EveryModStartsOn() => Assert.That(ModSwitches.Open(path).IsEnabled("test.mod"), Is.True);

        [Test]
        public void TurnedOffStaysOffNextLaunch()
        {
            ModSwitches.Open(path).SetEnabled("test.mod", false);

            var next = ModSwitches.Open(path);
            Assert.That(next.IsEnabled("test.mod"), Is.False);
            Assert.That(next.IsEnabled("test.other"), Is.True);
        }

        [Test]
        public void TurningBackOnSticks()
        {
            var switches = ModSwitches.Open(path);
            switches.SetEnabled("test.mod", false);
            switches.SetEnabled("test.mod", false);
            switches.SetEnabled("test.mod", true);

            Assert.That(ModSwitches.Open(path).IsEnabled("test.mod"), Is.True);
        }

        [Test]
        public void NothingIsWrittenUntilSomethingChanges()
        {
            ModSwitches.Open(path).SetEnabled("test.mod", true);
            Assert.That(File.Exists(path), Is.False);
        }

        [Test]
        public void UnreadableFileTurnsNothingOff()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{ not json");

            ModSwitches? switches = null;
            Assert.DoesNotThrow(() => switches = ModSwitches.Open(path));
            Assert.That(switches!.IsEnabled("test.mod"), Is.True);
        }
    }
}
