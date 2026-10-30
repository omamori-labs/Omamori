using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Events;
using Omamori.Core.Events.Sources;
using Omamori.Core.Features;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class GameEventsTests
    {
        [TearDown]
        public void TearDown()
        {
            GameEvents.Clear();
            FeatureRegistry.Set(Feature.ScreenEvents, FeatureState.Pending, null);
            FeatureRegistry.Set(Feature.LifecycleEvents, FeatureState.Pending, null);
        }

        [Test]
        public void UnavailableHookIsSkippedNotThrown()
        {
            var log = Substitute.For<IModLogger>();
            bool called = false;

            new GameEvents(manifest(), log).OnScreenChanged(_ => called = true);
            GameEvents.Raise(GameEvent.ScreenChanged, new ScreenChange());

            Assert.That(called, Is.False);
            log.Received(1).Info(Arg.Is<string>(m => m.Contains("isn't available", StringComparison.Ordinal)));
        }

        [Test]
        public void ThrowingHandlerDoesNotStopTheRest()
        {
            FeatureRegistry.Set(Feature.ScreenEvents, FeatureState.Active, null);

            var events = new GameEvents(manifest(), Substitute.For<IModLogger>());
            ScreenChange? received = null;

            events.OnScreenChanged(_ => throw new InvalidOperationException("broken mod"));
            events.OnScreenChanged(change => received = change);

            var raised = new ScreenChange { Screen = GameScreen.SongSelect };
            Assert.DoesNotThrow(() => GameEvents.Raise(GameEvent.ScreenChanged, raised));
            Assert.That(received, Is.EqualTo(raised));
        }

        [Test]
        public void OnlyTheRaisedEventsHandlersRun()
        {
            FeatureRegistry.Set(Feature.ScreenEvents, FeatureState.Active, null);
            int calls = 0;

            new GameEvents(manifest(), Substitute.For<IModLogger>()).OnScreenChanged(_ => calls++);
            GameEvents.Raise(GameEvent.PlayEnded, new PlayResult());

            Assert.That(calls, Is.Zero);
        }

        // song select swaps in a new copy of the same beatmap, the window reports the same state twice and so on
        [Test]
        public void RaiseChangedDropsAnEqualValue()
        {
            FeatureRegistry.Set(Feature.LifecycleEvents, FeatureState.Active, null);
            int calls = 0;

            new GameEvents(manifest(), Substitute.For<IModLogger>()).OnWindowChanged(_ => calls++);
            GameEvents.RaiseChanged(GameEvent.WindowChanged, new WindowDetails { Focused = true });
            GameEvents.RaiseChanged(GameEvent.WindowChanged, new WindowDetails { Focused = true });
            GameEvents.RaiseChanged(GameEvent.WindowChanged, new WindowDetails { Focused = false });

            Assert.That(calls, Is.EqualTo(2));
        }

        // closing from the window asks to exit, then osu! exits again after its outro
        [Test]
        public void ExitingIsRaisedOnce()
        {
            FeatureRegistry.Set(Feature.LifecycleEvents, FeatureState.Active, null);
            int calls = 0;

            new GameEvents(manifest(), Substitute.For<IModLogger>()).OnGameExiting(() => calls++);
            LifecycleEventSource.Exiting();
            LifecycleEventSource.Exiting();

            Assert.That(calls, Is.EqualTo(1));
        }

        private static ModManifest manifest() => new ModManifest
        {
            Id = "test.events",
            Hooks = new Dictionary<Hook, HookRequirement> { [Hook.ScreenEvents] = HookRequirement.Optional },
        };
    }
}
