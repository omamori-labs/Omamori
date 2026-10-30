using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core;

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

        private static ModManifest manifest() => new ModManifest
        {
            Id = "test.events",
            Hooks = new Dictionary<Hook, HookRequirement> { [Hook.ScreenEvents] = HookRequirement.Optional },
        };
    }
}
