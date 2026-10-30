using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Omamori.API;

namespace Omamori.Core
{
    internal static class FeatureRegistry
    {
        public static readonly IReadOnlyList<FeatureInfo> ALL =
        [
            new FeatureInfo(Feature.HideHostRuleset, "hide-host-ruleset", "Hide loader ruleset", "an unplayable \"Omamori\" mode shows in the ruleset list"),
            new FeatureInfo(Feature.ModLoading, "mod-loading", "Mod loading", "no mods load"),
            new FeatureInfo(Feature.MainMenuUi, "main-menu-ui", "Main menu UI", "mod UI on the main menu doesn't show", Hook.MainMenu),
            new FeatureInfo(Feature.GlobalOverlay, "global-overlay", "Global overlay", "mod overlays don't show", Hook.GlobalOverlay),
            new FeatureInfo(Feature.CrashReportFilter, "crash-report-filter", "Crash report filter", "crashes caused by mods may be reported to ppy as osu! bugs"),
            new FeatureInfo(Feature.ModContainment, "mod-containment", "Mod crash protection", "a crashing mod can crash osu! instead of being turned off"),
            new FeatureInfo(Feature.SettingsModList, "settings-mod-list", "Mod list in settings", "mods can't be seen or turned off from settings"),
            new FeatureInfo(Feature.ModManager, "mod-manager", "Mod manager", "mods can't be managed in game, only by editing the mods folder"),
            new FeatureInfo(Feature.ScreenEvents, "screen-events", "Screen events", "mods aren't told when the screen changes", Hook.ScreenEvents),
            new FeatureInfo(Feature.BeatmapEvents, "beatmap-events", "Beatmap events", "mods aren't told when the beatmap, ruleset or selected mods change", Hook.BeatmapEvents),
            new FeatureInfo(Feature.PlayEvents, "play-events", "Play events", "mods aren't told when a play starts, pauses or ends", Hook.PlayEvents),
            new FeatureInfo(Feature.LifecycleEvents, "lifecycle-events", "Lifecycle events", "mods aren't told when osu! closes or the window changes", Hook.LifecycleEvents),
            new FeatureInfo(Feature.MusicEvents, "music-events", "Music events", "mods aren't told when the music changes", Hook.MusicEvents),
            new FeatureInfo(Feature.UserEvents, "user-events", "User events", "mods aren't told about logging in or new statistics", Hook.UserEvents),
            new FeatureInfo(Feature.SkinEvents, "skin-events", "Skin events", "mods aren't told when the skin changes", Hook.SkinEvents),
            new FeatureInfo(Feature.LibraryEvents, "library-events", "Library events", "mods aren't told about saved scores or added and removed beatmaps", Hook.LibraryEvents),
            new FeatureInfo(Feature.OverlayEvents, "overlay-events", "Overlay events", "mods aren't told when overlays open or close", Hook.OverlayEvents),
            new FeatureInfo(Feature.MultiplayerEvents, "multiplayer-events", "Multiplayer events", "mods aren't told about multiplayer rooms", Hook.MultiplayerEvents),
        ];

        private static readonly Dictionary<Feature, FeatureStatus> statuses = ALL.ToDictionary(f => f.Feature, f => new FeatureStatus(f, FeatureState.Pending, null));

        private static readonly object state_lock = new object();

        /// <summary>a feature threw and turned off, raised once per feature on whichever thread it threw</summary>
        public static event Action<Feature>? Faulted;

        public static IReadOnlyList<FeatureStatus> Statuses
        {
            get
            {
                lock (state_lock)
                    return [.. ALL.Select(f => statuses[f.Feature])];
            }
        }

        public static FeatureInfo Info(Feature feature) => ALL.First(f => f.Feature == feature);

        /// <summary>the feature backing each public <see cref="Hook"/>, from the table above</summary>
        public static Feature ForHook(Hook hook) =>
            ALL.FirstOrDefault(f => f.Hook == hook)?.Feature ?? throw new ArgumentOutOfRangeException(nameof(hook), hook, "no feature backs this hook");

        public static bool IsAvailable(Hook hook) => IsActive(ForHook(hook));

        public static bool IsActive(Feature feature)
        {
            lock (state_lock)
                return statuses[feature].State == FeatureState.Active;
        }

        /// <summary>
        ///     patches every feature on its own, a failing feature is rolled back fully and the rest still apply
        /// </summary>
        public static void ApplyAll(Harmony harmony)
        {
            var patchClasses = typeof(FeatureRegistry).Assembly.GetTypes()
                                                      .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0)
                                                      .ToList();

            foreach (var orphan in patchClasses.Where(t => feature(t) == null))
                LoaderLog.Error($"Patch class {orphan.FullName} has no [Feature] and was skipped.");

            foreach (var info in ALL)
            {
                var classes = patchClasses.Where(t => feature(t) == info.Feature).ToList();

                if (classes.Count == 0)
                {
                    Set(info.Feature, FeatureState.Failed, "no patch classes");
                    continue;
                }

                var result = Apply(harmony, classes, () => FaultInjection.ThrowIfInjected(FaultInjection.APPLY, info));
                Set(info.Feature, result.Error == null ? FeatureState.Active : FeatureState.Failed, result.Error == null ? null : describe(result.Error));

                // details only go to the log, the user gets one summary notification in report()
                if (result.Error == null)
                {
                    LoaderLog.Info($"  {info.Id}: active, {result.Applied} method(s) patched");
                }
                else
                {
                    LoaderLog.Info($"  {info.Id}: failed after patching {result.Applied} method(s), rolled back {result.RolledBack} patch(es)");
                    LoaderLog.Info(result.Error.ToString());
                }
            }

            report();
        }

        /// <summary>
        ///     runs patch code for a feature, if it throws the feature turns off
        /// </summary>
        public static void Run(Feature feature, Action action) => Run(feature, 0, _ =>
        {
            action();
            return 0;
        });

        /// <summary>
        ///     for patches that change a value, eg a postfix's ref __result
        ///     returns <paramref name="value"/> unchanged when the feature is off or the transform throws
        /// </summary>
        public static T Run<T>(Feature feature, T value, Func<T, T> transform)
        {
            if (!IsActive(feature))
                return value;

            try
            {
                FaultInjection.ThrowIfInjected(FaultInjection.RUNTIME, Info(feature));
                return transform(value);
            }
            catch (Exception e)
            {
                fault(feature, e);
                return value;
            }
        }

        private static void fault(Feature feature, Exception exception)
        {
            var info = Info(feature);

            lock (state_lock)
            {
                // only the first fault is reported
                if (statuses[feature].State != FeatureState.Active)
                    return;

                statuses[feature] = new FeatureStatus(info, FeatureState.Faulted, describe(exception));
            }

            LoaderLog.Info($"Feature {info.Id} threw and was turned off for this session.");
            LoaderLog.Info(exception.ToString());
            LoaderLog.Notify($"{info.Name} stopped working and was turned off: {info.Impact}.");

            Faulted?.Invoke(feature);
        }

        private static Feature? feature(Type patchClass) =>
            (patchClass.GetCustomAttributes(typeof(FeatureAttribute), false).FirstOrDefault() as FeatureAttribute)?.Feature;

        // removes exactly the patches these classes added
        /// <summary>
        ///     patches one feature's classes, if any of them throws (or <paramref name="afterPatch"/> does) everything they added is rolled back
        /// </summary>
        internal static ApplyResult Apply(Harmony harmony, IReadOnlyList<Type> classes, Action? afterPatch = null)
        {
            int applied = 0;

            try
            {
                foreach (var type in classes)
                    applied += harmony.CreateClassProcessor(type).Patch().Count;

                afterPatch?.Invoke();
                return new ApplyResult(applied, 0, null);
            }
            catch (Exception e)
            {
                return new ApplyResult(applied, rollback(harmony, classes), e);
            }
        }

        private static int rollback(Harmony harmony, IReadOnlyList<Type> classes) =>
            PatchRemoval.Remove(harmony.GetPatchedMethods(), p => p.owner == harmony.Id && classes.Contains(p.PatchMethod.DeclaringType!));

        internal static void Set(Feature feature, FeatureState state, string? reason)
        {
            lock (state_lock)
                statuses[feature] = new FeatureStatus(Info(feature), state, reason);
        }

        private static void report()
        {
            var failed = Statuses.Where(s => s.State != FeatureState.Active).ToList();

            LoaderLog.Info($"Features: {ALL.Count - failed.Count}/{ALL.Count} active.");

            if (failed.Count == 0)
                return;

            LoaderLog.Notify($"Some features don't work on this osu! version: {string.Join("; ", failed.Select(s => s.Info.Impact))}.");
        }

        private static string describe(Exception e) => (e.InnerException ?? e).Message;
    }
}
