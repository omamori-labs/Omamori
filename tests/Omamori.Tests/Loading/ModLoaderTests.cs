using System;
using System.IO;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Loading;
using Omamori.Core.ModUi;
using Omamori.Core.Safety;
using osu.Framework.Graphics.Containers;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ModLoaderTests
    {
        [TearDown]
        public void TearDown()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Pending, null);
            UiHooks.Clear();
            ModFaults.Clear();
        }

        [Test]
        public void LoadThatThrowsLeavesNothingRunning()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Active, null);

            var manifest = new ModManifest { Id = "test.load-throws", Name = "Load throws" };
            var loader = ModIsolation.CreateLoader(Path.Combine(AppContext.BaseDirectory, "Semver.dll"));

            Assert.Throws<InvalidOperationException>(() => ModLoader.RunLoad(manifest.Id, loader, () =>
            {
                new UiHooks(manifest, new ModLogger(manifest.Id)).AddGlobalOverlay(() => new Container());
                throw new InvalidOperationException("bad setting key");
            }));

            var overlay = new Container();
            UiHooks.Populate(Hook.GlobalOverlay, overlay);

            Assert.That(ModFaults.IsFaulted(manifest.Id), Is.True);
            Assert.That(overlay.Count, Is.Zero);
        }

        [Test]
        public void LoadThatWorksLeavesTheModOn()
        {
            var loader = ModIsolation.CreateLoader(Path.Combine(AppContext.BaseDirectory, "Semver.dll"));

            ModLoader.RunLoad("test.load-works", loader, () => { });

            Assert.That(ModFaults.IsFaulted("test.load-works"), Is.False);
        }
    }
}
