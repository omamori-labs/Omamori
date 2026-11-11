using System;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Loading;
using Omamori.Core.Presence;
using Omamori.Core.Safety;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ModPresenceTests
    {
        private int refreshes;

        [SetUp]
        public void SetUp()
        {
            FeatureRegistry.Set(Feature.DiscordPresence, FeatureState.Active, null);
            ModPresence.Attach(() => refreshes++);
            refreshes = 0;
        }

        [TearDown]
        public void TearDown() => CoreState.Reset();

        [Test]
        public void NothingSetLeavesOsusOwnPresence() => Assert.That(ModPresence.Current(), Is.Null);

        [Test]
        public void MostRecentSetWins()
        {
            var first = presenceFor("test.first");
            var second = presenceFor("test.second");

            first.Set(new PresenceStatus { Details = "first" });
            second.Set(new PresenceStatus { Details = "second" });
            first.Set(new PresenceStatus { Details = "first again" });

            Assert.That(ModPresence.Current()?.Details, Is.EqualTo("first again"));
        }

        [Test]
        public void ClearingHandsBackToTheModBefore()
        {
            var first = presenceFor("test.first");
            var second = presenceFor("test.second");

            first.Set(new PresenceStatus { Details = "first" });
            second.Set(new PresenceStatus { Details = "second" });
            second.Clear();

            Assert.That(ModPresence.Current()?.Details, Is.EqualTo("first"));
        }

        [Test]
        public void StatusFromAModThatCrashedIsDropped()
        {
            var first = presenceFor("test.first");
            var second = presenceFor("test.second");

            first.Set(new PresenceStatus { Details = "first" });
            second.Set(new PresenceStatus { Details = "second" });
            ModFaults.TurnOff("test.second");

            Assert.That(ModPresence.Current()?.Details, Is.EqualTo("first"));
        }

        [Test]
        public void SettingAndClearingAskOsuToSendAgain()
        {
            var presence = presenceFor("test.mod");

            presence.Set(new PresenceStatus { Details = "playing" });
            presence.Clear();
            presence.Clear();

            Assert.That(refreshes, Is.EqualTo(2));
        }

        [Test]
        public void NothingIsSetWhenTheHookIsUnavailable()
        {
            FeatureRegistry.Set(Feature.DiscordPresence, FeatureState.Pending, null);

            presenceFor("test.mod").Set(new PresenceStatus { Details = "playing" });

            Assert.That(ModPresence.Current(), Is.Null);
            Assert.That(refreshes, Is.Zero);
        }

        [Test]
        public void ImageLongerThanDiscordAllowsIsRefused()
        {
            var status = new PresenceStatus { LargeImage = "https://example.com/" + new string('a', ModPresence.MAX_IMAGE_BYTES) };

            Assert.Throws<ArgumentException>(() => presenceFor("test.mod").Set(status));
            Assert.That(ModPresence.Current(), Is.Null);
        }

        private static ModPresence presenceFor(string id)
        {
            var manifest = new ModManifest { Id = id, Hooks = { [Hook.DiscordPresence] = HookRequirement.Optional } };
            return new ModPresence(manifest, new ModLogger(id));
        }
    }
}
