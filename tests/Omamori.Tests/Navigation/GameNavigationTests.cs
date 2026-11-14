using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Navigation;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class GameNavigationTests
    {
        [SetUp]
        public void SetUp() => FeatureRegistry.Set(Feature.Navigation, FeatureState.Active, null);

        [TearDown]
        public void TearDown() => CoreState.Reset();

        [Test]
        public void NothingHappensBeforeTheGameHasLoaded()
        {
            var navigation = navigationFor("test.navigation");

            Assert.That(navigation.SelectBeatmap(1), Is.False);
            Assert.That(navigation.PlayBeatmap(1), Is.False);
            Assert.That(navigation.ShowOverlay(GameOverlay.Settings), Is.False);
            Assert.DoesNotThrow(() =>
            {
                navigation.HideOverlays();
                navigation.ShowBeatmapPage(1);
                navigation.SearchBeatmaps("test");
                navigation.ShowUser(2);
                navigation.ShowWiki("Game_mode/osu!");
                navigation.OpenLink("https://osu.ppy.sh");
            });
        }

        [Test]
        public void OtherIsNeverAnOverlayToShow() => Assert.That(navigationFor("test.navigation").ShowOverlay(GameOverlay.Other), Is.False);

        [Test]
        public void SongSelectWithNothingPendingIsLeftAlone() => Assert.DoesNotThrow(() => GameNavigation.SongSelectEntered(null!));

        private static GameNavigation navigationFor(string id) => new GameNavigation(new ModManifest { Id = id }, Substitute.For<IModLogger>());
    }
}
