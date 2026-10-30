using System;
using System.IO;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Config;
using osu.Framework.Graphics;

namespace Omamori.Tests
{
    /// <summary>
    ///     each new store on the same path is a new launch of the game
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class ModConfigTests
    {
        private static readonly ModManifest mod = new ModManifest { Id = "test.settings" };

        private string path = null!;

        [SetUp]
        public void SetUp() => path = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}", "test.settings.json");

        [TearDown]
        public void TearDown()
        {
            ModConfig.Clear();

            string directory = Path.GetDirectoryName(path)!;
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        [Test]
        public void StartsAtTheDefault() => Assert.That(launch().Toggle("glow", "Glow", true).Value, Is.True);

        [Test]
        public void ChangesReachTheModLive()
        {
            var store = launch();
            var live = store.Number("size", "Size", 1, 0, 2);

            stored<double>(store, 0).Value = 1.5;

            Assert.That(live.Value, Is.EqualTo(1.5));
        }

        [Test]
        public void RestartOnlyChangesWaitForNextLaunch()
        {
            var store = launch();
            var live = store.Toggle("glow", "Glow", false, new SettingOptions { RequiresRestart = true });

            stored<bool>(store, 0).Value = true;

            Assert.That(live.Value, Is.False);
            Assert.That(store.Declared[0].Pending, Is.True);
            Assert.That(next().Toggle("glow", "Glow", false, new SettingOptions { RequiresRestart = true }).Value, Is.True);
        }

        [Test]
        public void ValuesSurviveALaunch()
        {
            var store = launch();
            store.Text("name", "Name");
            store.Choice("mode", "Mode", GameScreen.MainMenu);

            stored<string>(store, 0).Value = "hello";
            stored<GameScreen>(store, 1).Value = GameScreen.Results;

            var again = next();
            Assert.That(again.Text("name", "Name").Value, Is.EqualTo("hello"));
            Assert.That(again.Choice("mode", "Mode", GameScreen.MainMenu).Value, Is.EqualTo(GameScreen.Results));
        }

        [Test]
        public void StoredValueOutOfRangeFallsBackToTheDefault()
        {
            var store = launch();
            store.Number("size", "Size", 1, 0, 10);
            stored<double>(store, 0).Value = 8;

            Assert.That(next().Number("size", "Size", 1, 0, 5).Value, Is.EqualTo(1), "the mod lowered the max in an update");
        }

        [Test]
        public void UndeclaredKeysAreKept()
        {
            var store = launch();
            store.Toggle("old", "Old", false);
            stored<bool>(store, 0).Value = true;

            next().Toggle("new", "New", false);

            Assert.That(next().Toggle("old", "Old", false).Value, Is.True);
        }

        [Test]
        public void DuplicateKeyThrows()
        {
            var store = launch();
            store.Toggle("glow", "Glow", true);

            Assert.Throws<ArgumentException>(() => store.Toggle("glow", "Glow again", true));
        }

        [Test]
        public void BadRangeThrows() => Assert.Throws<ArgumentException>(() => launch().Number("size", "Size", 20, 0, 10));

        [Test]
        public void ThrowingHandlerDoesNotEscape()
        {
            var store = launch();
            var live = store.Toggle("glow", "Glow", false);
            live.BindValueChanged(_ => throw new InvalidOperationException("broken mod"));

            Assert.DoesNotThrow(() => stored<bool>(store, 0).Value = true);
        }

        [Test]
        public void ColourSurvivesALaunch()
        {
            var store = launch();
            store.Colour("tint", "Tint", Colour4.White);
            stored<Colour4>(store, 0).Value = Colour4.FromHex("ff66aa");

            Assert.That(next().Colour("tint", "Tint", Colour4.White).Value.ToHex(), Is.EqualTo(Colour4.FromHex("ff66aa").ToHex()));
        }

        [Test]
        public void StringChoiceThatWasRemovedFallsBackToTheDefault()
        {
            var store = launch();
            store.Choice("skin", "Skin", "a", ["a", "b"]);
            stored<string>(store, 0).Value = "b";

            Assert.That(next().Choice("skin", "Skin", "a", ["a", "c"]).Value, Is.EqualTo("a"));
        }

        [Test]
        public void StringChoiceDefaultMustBeAChoice() => Assert.Throws<ArgumentException>(() => launch().Choice("skin", "Skin", "x", ["a", "b"]));

        [Test]
        public void WholeNumberOutOfRangeFallsBackToTheDefault()
        {
            var store = launch();
            store.WholeNumber("count", "Count", 3, 1, 10);
            stored<int>(store, 0).Value = 9;

            Assert.That(next().WholeNumber("count", "Count", 3, 1, 5).Value, Is.EqualTo(3));
        }

        [Test]
        public void LayoutDoesNotTakeKeys()
        {
            var store = launch();
            store.Header("glow");
            store.Note("glow");
            store.Button("glow", "Reset", () => { });

            Assert.DoesNotThrow(() => store.Toggle("glow", "Glow", true));
            Assert.That(store.Declared, Has.Count.EqualTo(4));
        }

        private ModConfig launch() => new ModConfig(mod, path);

        // flushes what the last launch changed
        private ModConfig next()
        {
            ModConfig.FlushAll();
            return launch();
        }

        private static osu.Framework.Bindables.Bindable<T> stored<T>(ModConfig store, int index) => ((ConfigSetting<T>)store.Declared[index]).Stored;
    }
}
