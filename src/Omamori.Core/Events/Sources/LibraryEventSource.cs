using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.Core.Features;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Scoring;
using Realms;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     realm subscriptions, only made when a mod listens
    /// </summary>
    internal static class LibraryEventSource
    {
        private static readonly List<IDisposable> subscriptions = [];
        private static readonly BeatmapSetMirror sets = new BeatmapSetMirror();

        public static void Watch(OsuGame game)
        {
            if (GameEvents.HasHandlers(GameEvent.ScoreSaved))
                subscriptions.Add(game.realm.RegisterForNotifications(r => r.All<ScoreInfo>().Where(s => !s.DeletePending), scoresChanged));

            if (GameEvents.HasHandlers(GameEvent.BeatmapSetAdded) || GameEvents.HasHandlers(GameEvent.BeatmapSetRemoved))
                subscriptions.Add(game.realm.RegisterForNotifications(r => r.All<BeatmapSetInfo>().Where(s => !s.DeletePending && !s.Protected), setsChanged));
        }

        private static void scoresChanged(IRealmCollection<ScoreInfo> sender, ChangeSet? changes) => FeatureRegistry.Run(Feature.LibraryEvents, () =>
        {
            // the first call is the existing scores
            if (changes == null)
                return;

            foreach (int i in changes.InsertedIndices)
                GameEvents.Raise(GameEvent.ScoreSaved, EventDetails.Score(sender[i]));
        });

        private static void setsChanged(IRealmCollection<BeatmapSetInfo> sender, ChangeSet? changes) => FeatureRegistry.Run(Feature.LibraryEvents, () =>
        {
            // the first call, or realm resubscribing, is the whole library
            if (changes == null)
            {
                sets.Reset(sender.Select(EventDetails.BeatmapSet));
                return;
            }

            foreach (var removed in sets.Remove(changes.DeletedIndices))
                GameEvents.Raise(GameEvent.BeatmapSetRemoved, removed);

            var inserted = changes.InsertedIndices.Select(i => (i, EventDetails.BeatmapSet(sender[i]))).ToList();
            sets.Insert(inserted);

            foreach (var (_, added) in inserted.OrderBy(i => i.Item1))
                GameEvents.Raise(GameEvent.BeatmapSetAdded, added);

            sets.Replace(changes.NewModifiedIndices.Select(i => (i, EventDetails.BeatmapSet(sender[i]))));
        });
    }
}
