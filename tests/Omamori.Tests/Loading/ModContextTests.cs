using System.IO;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Loading;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ModContextTests
    {
        private string root = null!;

        [SetUp]
        public void SetUp() => root = TempDirectory.New();

        [TearDown]
        public void TearDown()
        {
            CoreState.Reset();
            TempDirectory.Delete(root);
        }

        [Test]
        public void DataDirectoryIsOnlyCreatedOnceTheModUsesIt()
        {
            string data = Path.Combine(root, "data", "test.mod");
            var context = new ModContext(new ModManifest { Id = "test.mod" }, Path.Combine(root, "mods", "test.mod"), data);

            Assert.That(Directory.Exists(data), Is.False);
            Assert.That(context.DataDirectory, Is.EqualTo(data));
            Assert.That(Directory.Exists(data), Is.True);
        }

        [Test]
        public void ShowingAPathWithoutTheGameDoesNothing()
        {
            var context = new ModContext(new ModManifest { Id = "test.mod" }, Path.Combine(root, "mods", "test.mod"), Path.Combine(root, "data", "test.mod"));

            Assert.That(context.ShowInFileBrowser(Path.GetTempPath()), Is.False);
        }
    }
}
