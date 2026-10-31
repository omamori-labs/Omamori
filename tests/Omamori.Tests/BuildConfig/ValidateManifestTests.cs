extern alias buildconfig;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using buildconfig::Omamori.ModBuildConfig;
using Microsoft.Build.Framework;
using NSubstitute;
using NUnit.Framework;

namespace Omamori.Tests
{
    [TestFixture]
    public class ValidateManifestTests
    {
        private const string valid = """{"id":"a.b","name":"A","version":"1.2.0","entry":"A.dll","apiVersion":"0.2","capabilities":["Cosmetic"]}""";

        private string directory = null!;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}");
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(directory, recursive: true);

        [Test]
        public void ValidManifestPassesAndReportsIdAndVersion()
        {
            var (ok, errors, task) = run(valid);

            Assert.That(ok, Is.True);
            Assert.That(errors, Is.Empty);
            Assert.That(task.ModId, Is.EqualTo("a.b"));
            Assert.That(task.ModVersion, Is.EqualTo("1.2.0"));
        }

        [Test]
        public void MissingManifest() => Assert.That(run(null).Errors, Has.Exactly(1).Matches<BuildErrorEventArgs>(e => e.Code == "OMA001"));

        [Test]
        public void BrokenJsonPointsAtTheLine()
        {
            var errors = run("{ \"id\": \"a\",\n  \"name\": }").Errors;

            Assert.That(errors.Single().Code, Is.EqualTo("OMA002"));
            Assert.That(errors.Single().LineNumber, Is.EqualTo(2));
        }

        [Test]
        public void SchemaProblemsAreEachAnError()
        {
            var errors = run(valid.Replace("\"Cosmetic\"", "\"cosmetic\"", StringComparison.Ordinal).Replace("}", ",\"dependancies\":[]}", StringComparison.Ordinal)).Errors;

            Assert.That(errors, Has.Count.EqualTo(2).And.All.Matches<BuildErrorEventArgs>(e => e.Code == "OMA003"));
        }

        [Test]
        public void EntryMustBeThisProjectsAssembly() =>
            Assert.That(run(valid, expectedEntry: "Other.dll").Errors.Single().Code, Is.EqualTo("OMA004"));

        [Test]
        public void ApiNewerThanTheBuildConfigFails() =>
            Assert.That(run(valid.Replace("\"0.2\"", "\"0.9\"", StringComparison.Ordinal)).Errors.Single().Code, Is.EqualTo("OMA005"));

        [Test]
        public void DependingOnItselfFails() =>
            Assert.That(run(valid.Replace("}", ",\"dependencies\":[{\"id\":\"a.b\"}]}", StringComparison.Ordinal)).Errors.Single().Code, Is.EqualTo("OMA007"));

        [Test]
        [Platform("Win")]
        public void LockedManifestIsAnErrorNotACrash()
        {
            string path = Path.Combine(directory, "mod.json");
            File.WriteAllText(path, valid);

            using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

            Assert.That(run(null).Errors.Single().Code, Is.EqualTo("OMA006"));
        }

        [Test]
        public void ErrorsPointAtTheManifest() =>
            Assert.That(run(valid, expectedEntry: "Other.dll").Errors.Single().File, Is.EqualTo(Path.Combine(directory, "mod.json")));

        private (bool Ok, List<BuildErrorEventArgs> Errors, ValidateManifest Task) run(string? manifest, string expectedEntry = "A.dll")
        {
            string path = Path.Combine(directory, "mod.json");
            if (manifest != null)
                File.WriteAllText(path, manifest);

            var errors = new List<BuildErrorEventArgs>();
            var engine = Substitute.For<IBuildEngine>();
            engine.When(e => e.LogErrorEvent(Arg.Any<BuildErrorEventArgs>())).Do(c => errors.Add(c.Arg<BuildErrorEventArgs>()));

            var task = new ValidateManifest { BuildEngine = engine, ManifestPath = path, ExpectedEntry = expectedEntry };
            return (task.Execute(), errors, task);
        }
    }
}
