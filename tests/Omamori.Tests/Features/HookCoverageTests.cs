using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;

namespace Omamori.Tests
{
    [TestFixture]
    public class HookCoverageTests
    {
        [Test]
        public void EveryHookHasAFeature([Values] Hook hook) => Assert.DoesNotThrow(() => FeatureRegistry.ForHook(hook));

        [Test]
        public void SchemaListsEveryHook()
        {
            var schema = JsonNode.Parse(File.ReadAllText(Path.Combine(TestSetup.RepoRoot, "schemas", "mod.schema.json")))!;
            var listed = schema["properties"]!["hooks"]!["propertyNames"]!["enum"]!.AsArray().Select(n => n!.GetValue<string>());

            Assert.That(listed, Is.EquivalentTo(Enum.GetNames<Hook>()));
        }
    }
}
