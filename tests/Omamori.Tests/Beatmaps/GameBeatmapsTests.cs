using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Beatmaps;
using Omamori.Core.Features;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class GameBeatmapsTests
    {
        [SetUp]
        public void SetUp() => FeatureRegistry.Set(Feature.Beatmaps, FeatureState.Active, null);

        [TearDown]
        public void TearDown() => CoreState.Reset();

        [Test]
        public async Task NothingIsReadBeforeTheGameHasLoaded()
        {
            var beatmaps = beatmapsFor("test.beatmaps");

            Assert.That(await beatmaps.GetCurrentAsync(), Is.Null);
            Assert.That(await beatmaps.GetCurrentDifficultyAsync(), Is.Null);
        }

        [Test]
        public async Task UnavailableHookReadsNothing()
        {
            FeatureRegistry.Set(Feature.Beatmaps, FeatureState.Failed, "test");

            Assert.That(await beatmapsFor("test.beatmaps").GetCurrentAsync(), Is.Null);
        }

        private static GameBeatmaps beatmapsFor(string id) => new GameBeatmaps(new ModManifest { Id = id }, Substitute.For<IModLogger>());
    }
}
