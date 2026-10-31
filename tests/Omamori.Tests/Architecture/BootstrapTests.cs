using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Omamori.Bootstrap;

namespace Omamori.Tests
{
    [TestFixture]
    public class BootstrapTests
    {
        private static readonly MethodInfo start = typeof(OmamoriRuleset).Assembly.GetType("Omamori.Bootstrap.Bootstrap", throwOnError: true)!
                                                                          .GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Static)!;

        private string directory = null!;

        [SetUp]
        public void SetUp() => directory = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}");

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        [Test]
        public void MissingCoreLeavesOsuRunning()
        {
            Directory.CreateDirectory(directory);

            Assert.DoesNotThrow(() => start.Invoke(null, [directory]));
        }

        [Test]
        public void UnknownLocationLeavesOsuRunning() => Assert.DoesNotThrow(() => start.Invoke(null, [null]));

        [Test]
        public void UnreadableCoreLeavesOsuRunning()
        {
            string core = Path.Combine(directory, "omamori", "core", "Omamori.Core.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(core)!);
            File.WriteAllText(core, "not an assembly");

            Assert.DoesNotThrow(() => start.Invoke(null, [directory]));
        }
    }
}
