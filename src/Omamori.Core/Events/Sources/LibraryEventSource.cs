using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Scoring;
using Realms;

namespace Omamori.Core
{
    /// <summary>
    ///     realm subscriptions, only made when a mod listens
    ///     realm's deletion indices point at rows that are already gone, so the set list is mirrored to describe removed sets
    /// </summary>
    internal static class LibraryEventSource
    {
        private static readonly List<IDisposable> subscriptions = [];
        private static List<BeatmapSetDetails> sets = [];

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
                sets = [.. sender.Select(EventDetails.BeatmapSet)];
                return;
            }

            foreach (int i in changes.DeletedIndices.OrderDescending())
            {
                var removed = sets[i];
                sets.RemoveAt(i);
                GameEvents.Raise(GameEvent.BeatmapSetRemoved, removed);
            }

            foreach (int i in changes.InsertedIndices.Order())
            {
                var added = EventDetails.BeatmapSet(sender[i]);
                sets.Insert(i, added);
                GameEvents.Raise(GameEvent.BeatmapSetAdded, added);
            }

            foreach (int i in changes.NewModifiedIndices)
                sets[i] = EventDetails.BeatmapSet(sender[i]);
        });
    }
}
