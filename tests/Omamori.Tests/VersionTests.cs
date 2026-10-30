using NUnit.Framework;
using Omamori.Core;

namespace Omamori.Tests
{
    [TestFixture]
    public class VersionTests
    {
        [TestCase("1.0.0")]
        [TestCase("1.2")]
        [TestCase("1")]
        [TestCase(" 1.2.0 ")]
        [TestCase("v1.2.0")]
        [TestCase("V1.2.0")]
        [TestCase("0.0.0")]
        [TestCase("1.0.0-beta")]
        [TestCase("1.0.0-beta.2+build.5")]
        [TestCase("1.0.0+20130313144700")]
        [TestCase("1.0.0-x-y-z.--")]
        [TestCase("1.0.0-0")]
        [TestCase("2147483648.0.0")]
        public void Valid(string version) => Assert.That(ModVersions.TryParse(version, out _), Is.True);

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        [TestCase("1.")]
        [TestCase(".1")]
        [TestCase("1..0")]
        [TestCase("01.2.0")]
        [TestCase("1.02.0")]
        [TestCase("1.2.3.4")]
        [TestCase("1.0.0-")]
        [TestCase("1.0.0+")]
        [TestCase("1.0.0-01")]
        [TestCase("-1.0.0")]
        [TestCase("+1.0.0")]
        [TestCase("1.0.0-be ta")]
        [TestCase("abc")]
        [TestCase("1.0.0+bu_ild")]
        [TestCase("1 .0")]
        [TestCase("1.0 .0")]
        [TestCase("v")]
        [TestCase("vv1.0.0")]
        [TestCase("1.0.0-beta..1")]
        [TestCase("1.0.0+a..b")]
        public void Invalid(string? version) => Assert.That(ModVersions.TryParse(version, out _), Is.False);

        [TestCase("1.2", "1.2.0", 0)]
        [TestCase("1", "1.0.0", 0)]
        [TestCase("1.0.0+a", "1.0.0+b", 0)]
        [TestCase("1.0.0-beta", "1.0.0", -1)]
        [TestCase("1.10.0", "1.9.0", 1)]
        [TestCase("2.0.0", "1.99.99", 1)]
        public void Precedence(string a, string b, int expected) => Assert.That(compare(a, b), Is.EqualTo(expected));

        [Test]
        public void SemverOrgPrecedenceChain()
        {
            string[] chain = ["1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta", "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0"];

            for (int i = 0; i < chain.Length - 1; i++)
            {
                Assert.That(compare(chain[i], chain[i + 1]), Is.Negative, $"{chain[i]} < {chain[i + 1]}");
                Assert.That(compare(chain[i + 1], chain[i]), Is.Positive, $"{chain[i + 1]} > {chain[i]}");
            }
        }

        // loader provides 0.2
        [TestCase("0.2", true)]
        [TestCase("0.1", true)]
        [TestCase("0.0", true)]
        [TestCase("0.3", false)]
        [TestCase("1.0", false)]
        [TestCase("1.2", false)]
        [TestCase("00.2", false)]
        [TestCase("x.y", false)]
        [TestCase(null, false)]
        [TestCase("", false)]
        public void ApiCompatibility(string? apiVersion, bool compatible) =>
            Assert.That(ApiVersions.IsCompatible(apiVersion, out _), Is.EqualTo(compatible));

        [Test]
        public void ApiTooNewSaysWhatToDo()
        {
            ApiVersions.IsCompatible("0.3", out string? problem);
            Assert.That(problem, Does.Contain("needs a newer Omamori"));
        }

        private static int compare(string a, string b)
        {
            Assert.That(ModVersions.TryParse(a, out var left), Is.True, a);
            Assert.That(ModVersions.TryParse(b, out var right), Is.True, b);
            return System.Math.Sign(left!.ComparePrecedenceTo(right));
        }
    }
}
