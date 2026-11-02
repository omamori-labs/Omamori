using DiscordRPC;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Patching;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class DiscordPresenceStatusTests
    {
        private RichPresence presence = null!;

        [SetUp]
        public void SetUp()
        {
            Patches.DiscordPresenceStatus.Reset();
            presence = new RichPresence { Assets = new Assets { LargeImageKey = "osu_logo_lazer", SmallImageKey = "ruleset_0" } };
        }

        [TearDown]
        public void TearDown() => Patches.DiscordPresenceStatus.Reset();

        [Test]
        public void OnlyWhatTheModSetIsReplaced()
        {
            update("osu! details", new PresenceStatus { State = "mod state", LargeImage = "https://example.com/cover.jpg" });

            Assert.That(presence.Details, Is.EqualTo("osu! details"));
            Assert.That(presence.State, Is.EqualTo("mod state"));
            Assert.That(presence.Assets.LargeImageKey, Is.EqualTo("https://example.com/cover.jpg"));
            Assert.That(presence.Assets.SmallImageKey, Is.EqualTo("ruleset_0"));
        }

        [Test]
        public void ImageGoesBackToOsusOnceTheModClears()
        {
            update("osu! details", new PresenceStatus { LargeImage = "https://example.com/cover.jpg" });
            update("osu! details", null);

            Assert.That(presence.Assets.LargeImageKey, Is.EqualTo("osu_logo_lazer"));
        }

        [Test]
        public void HiddenInformationKeepsOsusImageAfterAModsWasShown()
        {
            update("osu! details", new PresenceStatus { LargeImage = "https://example.com/cover.jpg" });

            // what the patch passes when privacy is Limited
            update("osu! details", null);

            Assert.That(presence.Assets.LargeImageKey, Is.EqualTo("osu_logo_lazer"));
        }

        [Test]
        public void LongTextIsCutToFit()
        {
            update("osu! details", new PresenceStatus { Details = new string('a', 500) });

            Assert.That(System.Text.Encoding.UTF8.GetByteCount(presence.Details), Is.LessThanOrEqualTo(128));
        }

        private void update(string osuDetails, PresenceStatus? status)
        {
            Patches.DiscordPresenceStatus.Restore(presence);
            presence.Details = osuDetails;
            Patches.DiscordPresenceStatus.Apply(presence, status);
        }
    }
}
