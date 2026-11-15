using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Loading;

namespace Omamori.Tests
{
    [TestFixture]
    public class ApiVersionsTests
    {
        // loader provides 1.0
        [TestCase("1.0", true)]
        [TestCase("1.1", false)]
        [TestCase("0.7", false)]
        [TestCase("0.0", false)]
        [TestCase("2.0", false)]
        [TestCase("00.2", false)]
        [TestCase("x.y", false)]
        [TestCase(null, false)]
        [TestCase("", false)]
        public void ApiCompatibility(string? apiVersion, bool compatible) =>
            Assert.That(ApiVersions.IsCompatible(apiVersion, ApiInfo.VERSION, out _), Is.EqualTo(compatible));

        [Test]
        public void ApiTooNewSaysWhatToDo()
        {
            ApiVersions.IsCompatible("1.1", ApiInfo.VERSION, out string? problem);
            Assert.That(problem, Does.Contain("needs a newer Omamori"));
        }
    }
}
