using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.ModUi;
using Omamori.Core.Patching;
using Omamori.Core.Safety;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ContextMenusTests
    {
        private static readonly BeatmapDetails beatmap = new BeatmapDetails { Title = "test map" };

        [SetUp]
        public void SetUp() => FeatureRegistry.Set(Feature.ContextMenus, FeatureState.Active, null);

        [TearDown]
        public void TearDown() => CoreState.Reset();

        [Test]
        public void ModItemsComeAfterOsusOwnBehindASpacer()
        {
            hooks("test.menus").AddBeatmapMenuItem("Practice", _ => { });
            MenuItem own = new OsuMenuItem("Edit");

            var items = ContextMenus.Items(MenuTarget.Beatmap, beatmap, [own]);

            Assert.That(items[0], Is.SameAs(own));
            Assert.That(items[1], Is.InstanceOf<OsuMenuItemSpacer>());
            Assert.That(items[2].Text.Value.ToString(), Is.EqualTo("Practice"));
        }

        [Test]
        public void EmptyMenuGetsOnlyModItems()
        {
            hooks("test.menus").AddBeatmapMenuItem("Practice", _ => { });

            Assert.That(ContextMenus.Items(MenuTarget.Beatmap, beatmap, []), Has.Length.EqualTo(1));
        }

        [Test]
        public void ItemsOnlyGoOnTheirOwnKindOfPanel()
        {
            var ui = hooks("test.menus");
            ui.AddScoreMenuItem("Export", _ => { });
            ui.AddBeatmapSetMenuItem("Open folder", _ => { });

            Assert.That(ContextMenus.Items(MenuTarget.Beatmap, beatmap, []), Is.Empty);
        }

        [Test]
        public void ClickingHandsTheModTheDetails()
        {
            BeatmapDetails? clicked = null;
            hooks("test.menus").AddBeatmapMenuItem("Practice", details => clicked = details);

            ContextMenus.Items(MenuTarget.Beatmap, beatmap, [])[0].Action.Value!.Invoke();

            Assert.That(clicked, Is.SameAs(beatmap));
        }

        [Test]
        public void FaultedModsItemsAreLeftOut()
        {
            hooks("test.broken").AddBeatmapMenuItem("Broken", _ => { });
            hooks("test.working").AddBeatmapMenuItem("Working", _ => { });
            ModFaults.TurnOff("test.broken");

            var items = ContextMenus.Items(MenuTarget.Beatmap, beatmap, []);

            Assert.That(items.Select(i => i.Text.Value.ToString()), Is.EqualTo(["Working"]));
        }

        [Test]
        public void UnknownPanelIsLeftAlone()
        {
            hooks("test.menus").AddBeatmapMenuItem("Practice", _ => { });
            MenuItem[] own = [new OsuMenuItem("Edit")];

            Assert.That(ContextMenus.Extend(new object(), own), Is.SameAs(own));
        }

        [Test]
        public void UnavailableHookSkipsTheItem()
        {
            FeatureRegistry.Set(Feature.ContextMenus, FeatureState.Failed, "test");

            hooks("test.menus").AddBeatmapMenuItem("Practice", _ => { });

            Assert.That(ContextMenus.Items(MenuTarget.Beatmap, beatmap, []), Is.Empty);
        }

        [Test]
        public void EveryPanelsMenuGetterIsFound()
        {
            var targets = Patches.ContextMenuInjection.Targets().ToList();

            Assert.That(targets, Has.Count.EqualTo(5));
            Assert.That(targets, Has.All.Not.Null);
            Assert.That(targets.Select(t => t.DeclaringType), Is.Unique);
        }

        private static UiHooks hooks(string id) => new UiHooks(new ModManifest { Id = id }, Substitute.For<IModLogger>());
    }
}
