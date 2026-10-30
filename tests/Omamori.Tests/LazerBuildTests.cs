using NUnit.Framework;
using Omamori.Core;

namespace Omamori.Tests
{
    [TestFixture]
    public class LazerBuildTests
    {
        [TestCase("2026.1005.0-lazer+9a10d935b05a3b896f4bd811d620f2023331db93", "2026.1005.0-lazer")]
        [TestCase("2026.1005.0-lazer", "2026.1005.0-lazer")]
        [TestCase(" 2026.1005.0-lazer ", "2026.1005.0-lazer")]
        [TestCase(null, "")]
        public void NormaliseDropsTheCommit(string? informational, string expected) =>
            Assert.That(LazerBuild.Normalise(informational), Is.EqualTo(expected));

        [Test]
        public void SameBuildIsNoMismatch() => Assert.That(LazerBuild.Mismatch("2026.1005.0-lazer", "2026.1005.0-lazer"), Is.Null);

        [Test]
        public void NewerLazerWarns() =>
            Assert.That(LazerBuild.Mismatch("2026.1005.0-lazer", "2026.1026.0-lazer"), Does.Contain("built for osu! 2026.1005.0-lazer but this is 2026.1026.0-lazer"));

        [Test]
        public void SameVersionOtherChannelWarns() => Assert.That(LazerBuild.Mismatch("2026.1026.0-lazer", "2026.1026.0-tachyon"), Is.Not.Null);

        [TestCase("", "2026.1005.0-lazer")]
        [TestCase("2026.1005.0-lazer", "")]
        public void NothingToCompareIsNoMismatch(string builtFor, string running) => Assert.That(LazerBuild.Mismatch(builtFor, running), Is.Null);

        [Test]
        public void RecordedBuildMatchesTheSandbox()
        {
            Assert.That(LazerBuild.BuiltFor, Is.Not.Empty);
            Assert.That(LazerBuild.Running, Is.EqualTo(LazerBuild.BuiltFor));
        }
    }
}
