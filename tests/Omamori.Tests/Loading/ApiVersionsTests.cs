using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Loading;

namespace Omamori.Tests
{
    [TestFixture]
    public class ApiVersionsTests
    {
        // loader provides 0.3
        [TestCase("0.3", true)]
        [TestCase("0.2", true)]
        [TestCase("0.1", true)]
        [TestCase("0.0", true)]
        [TestCase("0.4", false)]
        [TestCase("1.0", false)]
        [TestCase("1.2", false)]
        [TestCase("00.2", false)]
        [TestCase("x.y", false)]
        [TestCase(null, false)]
        [TestCase("", false)]
        public void ApiCompatibility(string? apiVersion, bool compatible) =>
            Assert.That(ApiVersions.IsCompatible(apiVersion, ApiInfo.VERSION, out _), Is.EqualTo(compatible));

        [Test]
        public void ApiTooNewSaysWhatToDo()
        {
            ApiVersions.IsCompatible("0.4", ApiInfo.VERSION, out string? problem);
            Assert.That(problem, Does.Contain("needs a newer Omamori"));
        }
    }
}
