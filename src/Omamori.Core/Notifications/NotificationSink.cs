using System.Collections.Generic;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;

namespace Omamori.Core.Notifications
{
    /// <summary>
    ///     mods load before the game, their notifications wait here until the overlay exists
    /// </summary>
    internal static class NotificationSink
    {
        private static readonly List<Notification> pending = [];
        private static readonly object sink_lock = new object();
        private static INotificationOverlay? overlay;

        public static void Attach(INotificationOverlay target)
        {
            List<Notification> queued;

            lock (sink_lock)
            {
                overlay = target;
                queued = [.. pending];
                pending.Clear();
            }

            foreach (var notification in queued)
                target.Post(notification);
        }

        public static void Post(Notification notification)
        {
            INotificationOverlay? target;

            lock (sink_lock)
            {
                target = overlay;

                if (target == null)
                {
                    pending.Add(notification);
                    return;
                }
            }

            target.Post(notification);
        }

        internal static void Reset()
        {
            lock (sink_lock)
            {
                overlay = null;
                pending.Clear();
            }
        }
    }
}
