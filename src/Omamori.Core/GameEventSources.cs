using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Online;
using osu.Game.Overlays;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Replays.Types;
using osu.Game.Scoring;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Play;
using osu.Game.Users;
using Realms;

namespace Omamori.Core
{
    /// <summary>
    ///     turns lazer's objects into the copies mods get, only public lazer state is read
    ///     null checks on realm objects use patterns, == needs realm's operators
    /// </summary>
    internal static partial class GameEventSources
    {
        private static readonly Dictionary<GameEvent, object> last = [];

        private static PlayDetails? activePlay;
        private static IBindable<bool>? paused;

        private static List<BeatmapSetDetails> sets = [];
        private static readonly List<IDisposable> subscriptions = [];

        public static void WatchScreens(OsuGame game)
        {
            game.ScreenStack.ScreenPushed += (last, next) => screenChanged(last, next);
            game.ScreenStack.ScreenExited += (last, next) => screenChanged(last, next);
        }

        public static void WatchBeatmap(OsuGame game)
        {
            game.Beatmap.BindValueChanged(e => FeatureRegistry.Run(Feature.BeatmapEvents, () =>
            {
                // song select swaps in a new working beatmap for the same map, raiseChanged drops the repeat
                if (e.NewValue is not DummyWorkingBeatmap)
                    raiseChanged(GameEvent.BeatmapChanged, Beatmap(e.NewValue.BeatmapInfo));
            }), true);

            game.Ruleset.BindValueChanged(e => FeatureRegistry.Run(Feature.BeatmapEvents, () =>
            {
                if (e.NewValue is { } ruleset)
                    raiseChanged(GameEvent.RulesetChanged, new RulesetDetails { ShortName = ruleset.ShortName, Name = ruleset.Name });
            }), true);

            IReadOnlyList<string> lastMods = [];

            game.SelectedMods.BindValueChanged(e => FeatureRegistry.Run(Feature.BeatmapEvents, () =>
            {
                // lazer reorders the same mods, that isn't a change
                var mods = acronyms(e.NewValue);
                if (mods.ToHashSet().SetEquals(lastMods))
                    return;

                lastMods = mods;
                GameEvents.Raise(GameEvent.ModsChanged, mods);
            }));
        }

        public static void WatchPlays(OsuGame game)
        {
            game.ScreenStack.ScreenPushed += (last, next) => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (last is Player finished && next is osu.Game.Screens.Ranking.ResultsScreen results && results.Score is { } score)
                    endPlay(finished, score);

                if (next is Player player)
                    startPlay(player);
            });

            // results exiting resumes the player, which isn't valid for resume and exits too, activePlay keeps that from ending twice
            game.ScreenStack.ScreenExited += (last, _) => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (last is Player player)
                    endPlay(player, null);
            });
        }

        public static void WatchWindow(OsuGame game)
        {
            void raise() => FeatureRegistry.Run(Feature.LifecycleEvents, () => raiseChanged(GameEvent.WindowChanged, new WindowDetails
            {
                Focused = game.Host.IsActive.Value,
                Fullscreen = game.Host.Window is { } window && window.WindowMode.Value != WindowMode.Windowed,
            }));

            // both can change off the update thread
            game.Host.IsActive.BindValueChanged(_ => game.Schedule(raise));
            game.Host.Window?.WindowMode.BindValueChanged(_ => game.Schedule(raise));
            raise();
        }

        private static bool exiting;

        // exit can be asked for more than once
        public static void Exiting()
        {
            if (exiting)
                return;

            exiting = true;
            GameEvents.Raise(GameEvent.GameExiting, true);
        }

        public static void WatchMusic(OsuGame game)
        {
            game.MusicController.TrackChanged += (beatmap, _) => FeatureRegistry.Run(Feature.MusicEvents, () =>
            {
                if (beatmap is not DummyWorkingBeatmap)
                    GameEvents.Raise(GameEvent.TrackChanged, Beatmap(beatmap.BeatmapInfo));
            });

            // IsPlaying has no event, so it's checked each frame
            game.Add(new MusicWatcher(game.MusicController));
        }

        public static void WatchUser(OsuGame game)
        {
            var api = game.API;

            void user() => FeatureRegistry.Run(Feature.UserEvents, () =>
            {
                var local = api.LocalUser.Value;
                bool loggedIn = api.IsLoggedIn;

                raiseChanged(GameEvent.UserChanged, new UserDetails
                {
                    LoggedIn = loggedIn,
                    Id = loggedIn ? local.Id : null,
                    Username = loggedIn ? local.Username : string.Empty,
                    Country = loggedIn && local.CountryCode != CountryCode.Unknown ? local.CountryCode.ToString() : string.Empty,
                });
            });

            // the api changes these from its own thread
            api.LocalUser.BindValueChanged(_ => game.Schedule(user));
            api.State.BindValueChanged(_ => game.Schedule(user));
            user();

            var statistics = game.Dependencies.Get<LocalUserStatisticsProvider>();
            statistics.StatisticsUpdated += update => game.Schedule(() => FeatureRegistry.Run(Feature.UserEvents, () =>
                GameEvents.Raise(GameEvent.StatisticsUpdated, new StatisticsUpdate
                {
                    Ruleset = update.Ruleset.ShortName,
                    Before = update.OldStatistics is { } before ? Statistics(before) : null,
                    After = Statistics(update.NewStatistics),
                })));
        }

        public static void WatchSkin(OsuGame game)
        {
            game.SkinManager.CurrentSkinInfo.BindValueChanged(e => FeatureRegistry.Run(Feature.SkinEvents, () =>
                raiseChanged(GameEvent.SkinChanged, e.NewValue.PerformRead(skin => new SkinDetails { Name = skin.Name, Creator = skin.Creator ?? string.Empty }))), true);
        }

        /// <summary>
        ///     realm subscriptions, only made when a mod listens, the set list is mirrored so removed sets can still be described
        /// </summary>
        public static void WatchLibrary(OsuGame game)
        {
            if (GameEvents.HasHandlers(GameEvent.ScoreSaved))
                subscriptions.Add(game.realm.RegisterForNotifications(r => r.All<ScoreInfo>().Where(s => !s.DeletePending), scoresChanged));

            if (GameEvents.HasHandlers(GameEvent.BeatmapSetAdded) || GameEvents.HasHandlers(GameEvent.BeatmapSetRemoved))
                subscriptions.Add(game.realm.RegisterForNotifications(r => r.All<BeatmapSetInfo>().Where(s => !s.DeletePending && !s.Protected), setsChanged));
        }

        public static void WatchOverlays(OsuGame game)
        {
            (VisibilityContainer? Overlay, GameOverlay Kind)[] overlays =
            [
                (game.Settings, GameOverlay.Settings),
                (game.Notifications, GameOverlay.Notifications),
                (game.chatOverlay, GameOverlay.Chat),
                (game.beatmapListing, GameOverlay.BeatmapListing),
                (game.beatmapSetOverlay, GameOverlay.BeatmapInfo),
                (game.dashboard, GameOverlay.Dashboard),
                (game.news, GameOverlay.News),
                (game.changelogOverlay, GameOverlay.Changelog),
                (game.wikiOverlay, GameOverlay.Wiki),
                (game.userProfile, GameOverlay.UserProfile),
            ];

            foreach (var (overlay, kind) in overlays)
            {
                overlay?.State.BindValueChanged(e => FeatureRegistry.Run(Feature.OverlayEvents, () =>
                    GameEvents.Raise(GameEvent.OverlayChanged, new OverlayChange { Overlay = kind, Visible = e.NewValue == Visibility.Visible })));
            }
        }

        public static void WatchMultiplayer(OsuGame game)
        {
            var client = game.MultiplayerClient;
            RoomDetails? current = null;

            client.RoomUpdated += () => game.Schedule(() => FeatureRegistry.Run(Feature.MultiplayerEvents, () =>
            {
                var room = client.Room is { } r
                    ? new RoomDetails
                    {
                        Id = r.RoomID,
                        Name = r.Settings.Name,
                        MatchType = r.Settings.MatchType.ToString(),
                        State = r.State.ToString(),
                        Users = r.Users.Count,
                        HasPassword = r.Settings.Password.Length > 0,
                    }
                    : null;

                if (room == current)
                    return;

                var kind = current == null ? RoomChangeKind.Joined : room == null ? RoomChangeKind.Left : RoomChangeKind.Updated;
                var reported = (room ?? current)!;

                current = room;
                GameEvents.Raise(GameEvent.RoomChanged, new RoomChange { Kind = kind, Room = reported });
            }));
        }

        private static void screenChanged(IScreen? last, IScreen? next) => FeatureRegistry.Run(Feature.ScreenEvents, () =>
            GameEvents.Raise(GameEvent.ScreenChanged, new ScreenChange
            {
                Screen = Kind(next),
                Previous = Kind(last),
                ScreenName = next?.GetType().Name ?? string.Empty,
            }));

        private static void startPlay(Player player)
        {
            if (activePlay != null)
                endPlay(player, null);

            activePlay = new PlayDetails
            {
                Beatmap = Beatmap(player.Beatmap.Value.BeatmapInfo),
                Ruleset = player.Ruleset.Value is { } ruleset ? ruleset.ShortName : string.Empty,
                Mods = acronyms(player.Mods.Value),
                IsReplay = player is ReplayPlayer or SpectatorPlayer,
            };

            GameEvents.Raise(GameEvent.PlayStarted, activePlay);

            // the clock only exists once the player has loaded
            if (player.GameplayClockContainer != null)
                watchPause(player);
            else
                player.OnLoadComplete += _ => watchPause(player);
        }

        // the clock starts paused and stops when the play ends, only pauses in between are reported
        private static void watchPause(Player player)
        {
            bool started = false;

            paused = player.GameplayClockContainer.IsPaused.GetBoundCopy();
            paused.BindValueChanged(e => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (player.GameplayState.HasCompleted)
                    return;

                if (!started)
                {
                    started = !e.NewValue;
                    return;
                }

                GameEvents.Raise(GameEvent.PlayPaused, e.NewValue);
            }));
        }

        private static void endPlay(Player player, ScoreInfo? score)
        {
            if (activePlay is not { } play)
                return;

            activePlay = null;
            paused?.UnbindAll();
            paused = null;

            bool detailed = score is not null && GameEvents.HasHandlers(GameEvent.PlayEnded);

            GameEvents.Raise(GameEvent.PlayEnded, new PlayResult
            {
                Play = play,
                Completed = score is not null,
                Score = score is null ? null : Score(score),
                Hits = detailed ? Hits(score!) : [],
                Replay = detailed ? replay(player) : [],
            });
        }

        private static void raiseChanged<T>(GameEvent gameEvent, T value)
            where T : class
        {
            if (last.TryGetValue(gameEvent, out object? previous) && EqualityComparer<T>.Default.Equals(previous as T, value))
                return;

            last[gameEvent] = value;
            GameEvents.Raise(gameEvent, value);
        }

        private static void scoresChanged(IRealmCollection<ScoreInfo> sender, ChangeSet? changes) => FeatureRegistry.Run(Feature.LibraryEvents, () =>
        {
            // the first call is the existing scores
            if (changes == null)
                return;

            foreach (int i in changes.InsertedIndices)
                GameEvents.Raise(GameEvent.ScoreSaved, Score(sender[i]));
        });

        private static void setsChanged(IRealmCollection<BeatmapSetInfo> sender, ChangeSet? changes) => FeatureRegistry.Run(Feature.LibraryEvents, () =>
        {
            // the first call, or realm resubscribing, is the whole library
            if (changes == null)
            {
                sets = [.. sender.Select(BeatmapSet)];
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
                var added = BeatmapSet(sender[i]);
                sets.Insert(i, added);
                GameEvents.Raise(GameEvent.BeatmapSetAdded, added);
            }

            foreach (int i in changes.NewModifiedIndices)
                sets[i] = BeatmapSet(sender[i]);
        });

        private static IReadOnlyList<ReplayFrameDetails> replay(Player player)
        {
            try
            {
                var beatmap = player.GameplayState.Beatmap;

                return
                [
                    .. player.Score.Replay.Frames.OfType<IConvertibleReplayFrame>()
                             .Select(f => f.ToLegacy(beatmap))
                             .Select(f => new ReplayFrameDetails { Time = f.Time, X = f.MouseX ?? 0, Y = f.MouseY ?? 0, Buttons = (ReplayButtons)f.ButtonState }),
                ];
            }
            catch (Exception e)
            {
                // a custom ruleset's frames can fail to convert, the rest of the result is still useful
                LoaderLog.Error("Couldn't convert the replay for a play ended event.", e);
                return [];
            }
        }

        internal static GameScreen Kind(IScreen? screen) => screen switch
        {
            MainMenu => GameScreen.MainMenu,
            osu.Game.Screens.Select.SongSelect => GameScreen.SongSelect,
            Player => GameScreen.Gameplay,
            osu.Game.Screens.Ranking.ResultsScreen => GameScreen.Results,
            osu.Game.Screens.Edit.Editor => GameScreen.Editor,
            osu.Game.Screens.OnlinePlay.Multiplayer.Multiplayer => GameScreen.Multiplayer,
            osu.Game.Screens.OnlinePlay.Playlists.Playlists => GameScreen.Playlists,
            _ => GameScreen.Other,
        };

        internal static BeatmapDetails Beatmap(BeatmapInfo beatmap) => new BeatmapDetails
        {
            Title = beatmap.Metadata.Title,
            Artist = beatmap.Metadata.Artist,
            Creator = beatmap.Metadata.Author is { } author ? author.Username : string.Empty,
            Difficulty = beatmap.DifficultyName,
            StarRating = beatmap.StarRating,
            Bpm = beatmap.BPM,
            Length = TimeSpan.FromMilliseconds(beatmap.Length),
            OnlineId = beatmap.OnlineID > 0 ? beatmap.OnlineID : null,
            Ruleset = beatmap.Ruleset is { } ruleset ? ruleset.ShortName : string.Empty,
        };

        internal static BeatmapSetDetails BeatmapSet(BeatmapSetInfo set) => new BeatmapSetDetails
        {
            Title = set.Metadata.Title,
            Artist = set.Metadata.Artist,
            Creator = set.Metadata.Author is { } author ? author.Username : string.Empty,
            OnlineId = set.OnlineID > 0 ? set.OnlineID : null,
            Difficulties = set.Beatmaps.Count,
        };

        internal static ScoreDetails Score(ScoreInfo score) => new ScoreDetails
        {
            TotalScore = score.TotalScore,
            Accuracy = score.Accuracy,
            Rank = score.Rank.ToString(),
            MaxCombo = score.MaxCombo,
            Pp = score.PP,
            Passed = score.Passed,
            Date = score.Date,
            Ruleset = score.Ruleset is { } ruleset ? ruleset.ShortName : string.Empty,
            // api mods don't need the ruleset instantiated
            Mods = [.. score.APIMods.Select(m => m.Acronym)],
            Beatmap = score.BeatmapInfo is { } beatmap ? Beatmap(beatmap) : null,
        };

        internal static IReadOnlyList<HitDetails> Hits(ScoreInfo score) =>
        [
            .. score.HitEvents.Select(h => new HitDetails
            {
                Time = h.HitObject.StartTime,
                Offset = h.TimeOffset,
                Result = h.Result.ToString(),
                X = h.Position?.X,
                Y = h.Position?.Y,
            }),
        ];

        internal static UserStatisticsDetails Statistics(UserStatistics statistics) => new UserStatisticsDetails
        {
            Pp = statistics.PP is { } pp ? (double)pp : null,
            GlobalRank = statistics.GlobalRank,
            CountryRank = statistics.CountryRank,
            Accuracy = statistics.Accuracy / 100,
            PlayCount = statistics.PlayCount,
            RankedScore = statistics.RankedScore,
        };

        private static IReadOnlyList<string> acronyms(IReadOnlyList<Mod>? mods) => [.. (mods ?? []).Select(m => m.Acronym)];

        private sealed partial class MusicWatcher : Component
        {
            private readonly MusicController music;
            private bool playing;

            public MusicWatcher(MusicController music)
            {
                this.music = music;
            }

            protected override void Update()
            {
                base.Update();

                if (music.IsPlaying == playing)
                    return;

                playing = music.IsPlaying;
                FeatureRegistry.Run(Feature.MusicEvents, () => GameEvents.Raise(GameEvent.MusicPlayingChanged, playing));
            }
        }
    }
}
