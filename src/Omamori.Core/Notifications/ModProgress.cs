using System;
using System.Threading;
using Omamori.API;
using osu.Game.Overlays.Notifications;

namespace Omamori.Core.Notifications
{
    internal sealed class ModProgress : IModProgress
    {
        private readonly ModManifest manifest;
        private readonly ProgressNotification notification;

        public ModProgress(ModManifest manifest, ProgressNotification notification)
        {
            this.manifest = manifest;
            this.notification = notification;
        }

        public CancellationToken Cancellation => notification.CancellationToken;

        public void Report(double progress, string? text = null)
        {
            if (text != null)
                notification.Text = ModNotifications.Label(manifest, text);

            notification.Progress = (float)Math.Clamp(progress, 0, 1);
        }

        public void Complete(string text)
        {
            notification.CompletionText = ModNotifications.Label(manifest, text);
            notification.State = ProgressNotificationState.Completed;
        }

        public void Fail(string text)
        {
            notification.Text = ModNotifications.Label(manifest, text);
            notification.State = ProgressNotificationState.Cancelled;
        }
    }
}
