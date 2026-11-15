using System;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Audio;
using Omamori.Core.Features;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class GameMusicTests
    {
        [SetUp]
        public void SetUp() => FeatureRegistry.Set(Feature.Audio, FeatureState.Active, null);

        [TearDown]
        public void TearDown() => CoreState.Reset();

        [Test]
        public void NothingHappensBeforeTheGameHasLoaded()
        {
            var music = musicFor("test.music");

            Assert.That(music.Pause(), Is.False);
            Assert.That(music.SeekTo(TimeSpan.FromSeconds(10)), Is.False);
            Assert.DoesNotThrow(() => music.Duck().Dispose());
        }

        [Test]
        public void NothingIsReadBeforeTheGameHasLoaded()
        {
            var music = musicFor("test.music");

            Assert.That(music.Current, Is.Null);
            Assert.That(music.IsPlaying, Is.False);
            Assert.That(music.Position, Is.EqualTo(TimeSpan.Zero));
            Assert.That(music.Length, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void DuckCanBeDisposedTwice()
        {
            var duck = musicFor("test.music").Duck();

            duck.Dispose();
            Assert.DoesNotThrow(duck.Dispose);
        }

        private static GameMusic musicFor(string id) => new GameMusic(new ModManifest { Id = id }, Substitute.For<IModLogger>());
    }
}
