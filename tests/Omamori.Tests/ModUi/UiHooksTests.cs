using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.ModUi;
using osu.Framework.Graphics.Containers;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class UiHooksTests
    {
        [TearDown]
        public void TearDown()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Pending, null);
            UiHooks.Clear();
        }

        [Test]
        public void UnavailableHookIsSkippedNotThrown()
        {
            var log = Substitute.For<IModLogger>();
            var hooks = new UiHooks(manifest(declared: true), log);

            Assert.DoesNotThrow(() => hooks.AddGlobalOverlay(() => new Container()));
            log.Received(1).Info(Arg.Is<string>(m => m.Contains("isn't available", StringComparison.Ordinal)));
        }

        [Test]
        public void UndeclaredHookIsHintedOnce()
        {
            var log = Substitute.For<IModLogger>();
            var hooks = new UiHooks(manifest(declared: false), log);

            hooks.AddGlobalOverlay(() => new Container());
            hooks.AddGlobalOverlay(() => new Container());

            log.Received(1).Info(Arg.Is<string>(m => m.Contains("without declaring it", StringComparison.Ordinal)));
        }

        [Test]
        public void DeclaredHookIsNotHinted()
        {
            var log = Substitute.For<IModLogger>();
            new UiHooks(manifest(declared: true), log).AddGlobalOverlay(() => new Container());

            log.DidNotReceive().Info(Arg.Is<string>(m => m.Contains("without declaring it", StringComparison.Ordinal)));
        }

        [Test]
        public void ThrowingFactoryDoesNotStopTheRest()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Active, null);

            var hooks = new UiHooks(manifest(declared: true), Substitute.For<IModLogger>());
            var marker = new Container();

            hooks.AddGlobalOverlay(() => throw new InvalidOperationException("broken mod"));
            hooks.AddGlobalOverlay(() => marker);

            var overlay = new Container();
            Assert.DoesNotThrow(() => UiHooks.Populate(Hook.GlobalOverlay, overlay));
            Assert.That(overlay, Does.Contain(marker));
        }

        private static ModManifest manifest(bool declared) => new ModManifest
        {
            Id = "test.ui",
            Hooks = declared ? new Dictionary<Hook, HookRequirement> { [Hook.GlobalOverlay] = HookRequirement.Optional } : [],
        };
    }
}
