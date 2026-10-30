using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Omamori.Core;
using static VerifyNUnit.Verifier;

namespace Omamori.Tests
{
    [TestFixture]
    public class ManifestSchemaTests
    {
        private const string minimal = """{"id":"a.b","name":"A","version":"1.0.0","entry":"A.dll","apiVersion":"0.2","capabilities":["Cosmetic"]}""";

        [Test]
        public void MinimalManifestIsValid() => Assert.That(validate(minimal), Is.Empty);

        [Test]
        public void FullManifestIsValid() => Assert.That(validate(with(minimal, """
            {"$schema":"x","author":"me","description":"d","hooks":{"MainMenu":"optional","GlobalOverlay":"required"},
             "dependencies":[{"id":"c.d","minVersion":"1.2"},{"id":"e-f_g","optional":true}]}
            """)), Is.Empty);

        /// <summary>the messages mod authors read</summary>
        [Test]
        public Task BrokenManifests() => Verify(new Dictionary<string, List<string>>
        {
            ["typo property"] = validate(with(minimal, """{"dependancies":[]}""")),
            ["lowercase enum"] = validate(with(minimal, """{"capabilities":["cosmetic"]}""")),
            ["integer enum"] = validate(with(minimal, """{"capabilities":[99]}""")),
            ["no capabilities"] = validate(with(minimal, """{"capabilities":[]}""")),
            ["nulls"] = validate(with(minimal, """{"version":null,"capabilities":null,"dependencies":[null]}""")),
            ["padded id"] = validate(with(minimal, """{"id":" A.B "}""")),
            ["uppercase id"] = validate(with(minimal, """{"id":"Yourname.Mod"}""")),
            ["padded name"] = validate(with(minimal, """{"name":" A "}""")),
            ["entry escapes folder"] = validate(with(minimal, """{"entry":"../A.dll"}""")),
            ["entry rooted"] = validate(with(minimal, """{"entry":"C:/A.dll"}""")),
            ["entry not a dll"] = validate(with(minimal, """{"entry":"A.exe"}""")),
            ["api version with patch"] = validate(with(minimal, """{"apiVersion":"0.2.0"}""")),
            ["unknown hook"] = validate(with(minimal, """{"hooks":{"MainMenuu":"optional"}}""")),
            ["bad requirement"] = validate(with(minimal, """{"hooks":{"MainMenu":"maybe"}}""")),
            ["dependency without id"] = validate(with(minimal, """{"dependencies":[{"minVersion":"1.0"}]}""")),
            ["leading zero minVersion"] = validate(with(minimal, """{"dependencies":[{"id":"c.d","minVersion":"01.0"}]}""")),
            ["missing required"] = validate("""{"id":"a.b"}"""),
        });

        [Test]
        public void MissingRequiredPropertiesAreReported() =>
            Assert.That(validate("""{"id":"a.b"}"""), Has.Some.Contains("Required properties"));

        [Test]
        public void SummaryCapsAtThreeProblems()
        {
            string summary = ManifestSchema.Summarise(["a", "b", "c", "d", "e"]);
            Assert.That(summary, Is.EqualTo("a; b; c (+2 more)"));
        }

        // the loader validates against an embedded copy, it must be the file editors use
        [Test]
        public void EmbeddedSchemaIsTheRepoFile()
        {
            using var stream = typeof(ManifestSchema).Assembly.GetManifestResourceStream("Omamori.Core.mod.schema.json")!;
            using var reader = new StreamReader(stream);

            Assert.That(reader.ReadToEnd(), Is.EqualTo(File.ReadAllText(Path.Combine(TestSetup.RepoRoot, "schemas", "mod.schema.json"))));
        }

        [TestCase("1.0.0")]
        [TestCase("1.2")]
        [TestCase("1")]
        [TestCase("v1.2.0")]
        [TestCase("V1.0.0")]
        [TestCase("1.0.0-beta.2+build.5")]
        [TestCase("1.0.0-x-y-z.--")]
        [TestCase("2147483648.0.0")]
        [TestCase("01.2.0")]
        [TestCase("1.2.3.4")]
        [TestCase("1.0.0-01")]
        [TestCase("1.0.0+bu_ild")]
        [TestCase("vv1.0.0")]
        [TestCase("1.0.0-beta..1")]
        [TestCase("\uFF11.0.0")]
        public void SemverPatternAgreesWithLibrary(string version)
        {
            var schema = JsonNode.Parse(File.ReadAllText(Path.Combine(TestSetup.RepoRoot, "schemas", "mod.schema.json")))!;
            var pattern = new Regex(schema["$defs"]!["semver"]!["pattern"]!.GetValue<string>());

            Assert.That(pattern.IsMatch(version), Is.EqualTo(ModVersions.TryParse(version, out _)));
        }

        private static List<string> validate(string json)
        {
            using var document = JsonDocument.Parse(json);
            return ManifestSchema.Validate(document.RootElement);
        }

        private static string with(string json, string overrides)
        {
            var node = JsonNode.Parse(json)!.AsObject();

            foreach (var (key, value) in JsonNode.Parse(overrides)!.AsObject().ToList())
                node[key] = value?.DeepClone();

            return node.ToJsonString();
        }
    }
}
