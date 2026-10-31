using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Notifications;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ModNotificationsTests
    {
        private INotificationOverlay overlay = null!;

        [SetUp]
        public void SetUp()
        {
            NotificationSink.Reset();
            overlay = Substitute.For<INotificationOverlay>();
        }

        [TearDown]
        public void TearDown()
        {
            NotificationSink.Reset();
            FeatureRegistry.Set(Feature.Notifications, FeatureState.Pending, null);
        }

        [Test]
        public void PostsBeforeTheGameLoadsWaitForTheOverlay()
        {
            FeatureRegistry.Set(Feature.Notifications, FeatureState.Active, null);
            var notifications = new ModNotifications(manifest(), Substitute.For<IModLogger>());

            notifications.Post("early");
            overlay.DidNotReceiveWithAnyArgs().Post(null!);

            NotificationSink.Attach(overlay);
            overlay.Received(1).Post(Arg.Is<Notification>(n => n.Text.ToString() == "Test: early"));

            notifications.Post("late");
            overlay.Received(1).Post(Arg.Is<Notification>(n => n.Text.ToString() == "Test: late"));
        }

        [Test]
        public void UnavailableHookPostsNothing()
        {
            NotificationSink.Attach(overlay);
            var notifications = new ModNotifications(manifest(), Substitute.For<IModLogger>());

            notifications.Post("hi");
            var progress = notifications.PostProgress("working");

            Assert.DoesNotThrow(() => progress.Report(0.5));
            overlay.DidNotReceiveWithAnyArgs().Post(null!);
        }

        [Test]
        public void ClickRunsTheModsAction()
        {
            FeatureRegistry.Set(Feature.Notifications, FeatureState.Active, null);
            NotificationSink.Attach(overlay);

            Notification? posted = null;
            overlay.Post(Arg.Do<Notification>(n => posted = n));

            bool clicked = false;
            new ModNotifications(manifest(), Substitute.For<IModLogger>()).Post("click me", () => clicked = true);

            Assert.That(posted!.Activated!(), Is.True);
            Assert.That(clicked, Is.True);
        }

        private static ModManifest manifest() => new ModManifest
        {
            Id = "test.notify",
            Name = "Test",
            Hooks = new Dictionary<Hook, HookRequirement> { [Hook.Notifications] = HookRequirement.Optional },
        };
    }
}
