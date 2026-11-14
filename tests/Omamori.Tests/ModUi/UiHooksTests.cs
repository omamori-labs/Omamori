using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.ModUi;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class UiHooksTests
    {
        [TearDown]
        public void TearDown() => CoreState.Reset();

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

        [Test]
        public void ThrowingFooterFactoryDoesNotStopTheFooter()
        {
            FeatureRegistry.Set(Feature.SongSelectFooter, FeatureState.Active, null);

            var hooks = new UiHooks(manifest(declared: true), Substitute.For<IModLogger>());
            hooks.AddToSongSelectFooter(() => throw new InvalidOperationException("broken mod"));

            Assert.That(UiHooks.FooterButtons(), Is.Empty);
        }

        [Test]
        public void FooterButtonsAreSkippedWhenTheHookIsUnavailable()
        {
            var hooks = new UiHooks(manifest(declared: true), Substitute.For<IModLogger>());

            Assert.DoesNotThrow(() => hooks.AddToSongSelectFooter(() => throw new InvalidOperationException("never built")));
            Assert.That(UiHooks.FooterButtons(), Is.Empty);
        }

        [Test]
        public void ToolbarButtonsGoJustBeforeTheUserButtonInLoadOrder()
        {
            FeatureRegistry.Set(Feature.Toolbar, FeatureState.Active, null);

            var music = new Container();
            var user = new Container();
            var row = new FillFlowContainer { Children = [music, user, new Container()] };

            var hooks = new UiHooks(manifest(declared: true), Substitute.For<IModLogger>());
            hooks.AddToolbarButton(FontAwesome.Solid.Star, "first", () => { });
            hooks.AddToolbarButton(FontAwesome.Solid.Heart, "second", () => { });

            UiHooks.PopulateToolbar(row, user);

            var added = row.Children.OfType<ModToolbarButton>().Select(row.GetLayoutPosition).ToList();

            Assert.That(added, Has.Count.EqualTo(2));
            Assert.That(added, Is.Ordered.Ascending);
            Assert.That(added, Has.All.GreaterThan(row.GetLayoutPosition(music)).And.All.LessThan(row.GetLayoutPosition(user)));
        }

        [Test]
        public void ToolbarIsLeftAloneWithoutButtons()
        {
            var user = new Container();
            var row = new FillFlowContainer { Children = [user] };

            UiHooks.PopulateToolbar(row, user);

            Assert.That(row.Children, Has.Count.EqualTo(1));
        }

        private static ModManifest manifest(bool declared) => new ModManifest
        {
            Id = "test.ui",
            Hooks = declared ? new Dictionary<Hook, HookRequirement> { [Hook.GlobalOverlay] = HookRequirement.Optional } : [],
        };
    }
}
