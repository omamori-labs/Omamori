using System;
using System.Collections.Generic;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Game.Overlays.Notifications;

namespace Omamori.Core.Notifications
{
    internal sealed class ModNotifications : IModNotifications
    {
        private readonly ModManifest manifest;
        private readonly IModLogger log;
        private readonly HashSet<Hook> hinted = [];

        public ModNotifications(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            this.log = log;
        }

        public void Post(string text, Action? onClick = null) => post(new SimpleNotification { Text = Label(manifest, text) }, onClick);

        public void PostError(string text, Action? onClick = null) => post(new SimpleErrorNotification { Text = Label(manifest, text) }, onClick);

        // an unavailable hook still hands back a progress, it's just never shown
        public IModProgress PostProgress(string text)
        {
            var notification = new ProgressNotification { Text = Label(manifest, text), State = ProgressNotificationState.Active };

            if (canPost())
                NotificationSink.Post(notification);

            return new ModProgress(manifest, notification);
        }

        public static string Label(ModManifest manifest, string text) => $"{manifest.Name}: {text}";

        private void post(SimpleNotification notification, Action? onClick)
        {
            if (!canPost())
                return;

            if (onClick != null)
            {
                string id = manifest.Id;

                notification.Activated = () =>
                {
                    ModFaults.RunForMod(id, "notification click", onClick);
                    return true;
                };
            }

            NotificationSink.Post(notification);
        }

        private bool canPost() => !ModFaults.IsFaulted(manifest.Id) && HookCheck.CanRegister(manifest, log, hinted, Hook.Notifications, "notification");
    }
}
