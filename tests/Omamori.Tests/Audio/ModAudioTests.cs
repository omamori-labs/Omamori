using System;
using System.IO;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Audio;
using Omamori.Core.Features;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ModAudioTests
    {
        private string folder = null!;

        [SetUp]
        public void SetUp()
        {
            FeatureRegistry.Set(Feature.Audio, FeatureState.Active, null);

            folder = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}");
            Directory.CreateDirectory(Path.Combine(folder, "mod", "sounds"));
            File.WriteAllText(Path.Combine(folder, "mod", "sounds", "ding.wav"), string.Empty);
            File.WriteAllText(Path.Combine(folder, "secret.wav"), string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            CoreState.Reset();
            Directory.Delete(folder, recursive: true);
        }

        [Test]
        public void SoundInTheModsFolderIsFound() =>
            Assert.That(ModAudio.InsideFolder(modFolder, "sounds/ding.wav"), Is.EqualTo(Path.Combine(modFolder, "sounds", "ding.wav")));

        [Test]
        public void PathOutOfTheModsFolderIsRefused() => Assert.That(ModAudio.InsideFolder(modFolder, "../secret.wav"), Is.Null);

        [Test]
        public void RootedPathIsRefused() => Assert.That(ModAudio.InsideFolder(modFolder, Path.Combine(folder, "secret.wav")), Is.Null);

        [Test]
        public void MissingSoundIsRefused() => Assert.That(ModAudio.InsideFolder(modFolder, "sounds/missing.wav"), Is.Null);

        [Test]
        public void NothingHappensBeforeTheGameHasLoaded()
        {
            var audio = audioFor("test.audio");

            Assert.That(audio.PlaySound("sounds/ding.wav"), Is.False);
            Assert.That(audio.PauseMusic(), Is.False);
            Assert.That(audio.SeekMusic(TimeSpan.FromSeconds(10)), Is.False);
            Assert.DoesNotThrow(() => audio.DuckMusic().Dispose());
        }

        [Test]
        public void DuckCanBeDisposedTwice()
        {
            var duck = audioFor("test.audio").DuckMusic();

            duck.Dispose();
            Assert.DoesNotThrow(duck.Dispose);
        }

        [Test]
        public void UnavailableHookPlaysNothing()
        {
            FeatureRegistry.Set(Feature.Audio, FeatureState.Failed, "test");

            Assert.That(audioFor("test.audio").PlaySound("sounds/ding.wav"), Is.False);
        }

        private string modFolder => Path.Combine(folder, "mod");

        private ModAudio audioFor(string id) => new ModAudio(new ModManifest { Id = id }, modFolder, Substitute.For<IModLogger>());
    }
}
