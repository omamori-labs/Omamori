using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Omamori.API;

namespace Omamori.Core
{
    /// <summary>
    ///     a group of patches that works or fail altogether
    /// </summary>
    internal enum Feature
    {
        HideHostRuleset,
        ModLoading,
        MainMenuUi,
        GlobalOverlay,
    }

    internal enum FeatureState
    {
        /// <summary>not applied yet</summary>
        Pending,

        Active,

        /// <summary>patching failed on startup, nothing of it is applied</summary>
        Failed,

        /// <summary>was active, its code threw while the game ran so it's off for the session</summary>
        Faulted,
    }

    /// <param name="Feature">the feature this describes</param>
    /// <param name="Id">stable kebab-case id for logs, fault injection and later the manager</param>
    /// <param name="Name">shown to users</param>
    /// <param name="Impact">what stops working when it's off, shown to users</param>
    internal sealed record FeatureInfo(Feature Feature, string Id, string Name, string Impact);

    internal sealed record FeatureStatus(FeatureInfo Info, FeatureState State, string? Reason);

    /// <summary>
    ///     marks a harmony patch class as part of a feature
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class FeatureAttribute(Feature feature) : Attribute
    {
        public Feature Feature { get; } = feature;
    }

    internal static class FeatureRegistry
    {
        public static readonly IReadOnlyList<FeatureInfo> ALL =
        [
            new FeatureInfo(Feature.HideHostRuleset, "hide-host-ruleset", "Hide loader ruleset", "an unplayable \"Omamori\" mode shows in the ruleset list"),
            new FeatureInfo(Feature.ModLoading, "mod-loading", "Mod loading", "no mods load"),
            new FeatureInfo(Feature.MainMenuUi, "main-menu-ui", "Main menu UI", "mod UI on the main menu doesn't show"),
            new FeatureInfo(Feature.GlobalOverlay, "global-overlay", "Global overlay", "mod overlays don't show"),
        ];

        private static readonly Dictionary<Feature, FeatureStatus> statuses = ALL.ToDictionary(f => f.Feature, f => new FeatureStatus(f, FeatureState.Pending, null));

        private static readonly object state_lock = new object();

        public static IReadOnlyList<FeatureStatus> Statuses
        {
            get
            {
                lock (state_lock)
                    return [.. ALL.Select(f => statuses[f.Feature])];
            }
        }

        public static FeatureInfo Info(Feature feature) => ALL.First(f => f.Feature == feature);

        /// <summary>the feature backing each public <see cref="Hook"/></summary>
        public static Feature ForHook(Hook hook) => hook switch
        {
            Hook.MainMenu => Feature.MainMenuUi,
            Hook.GlobalOverlay => Feature.GlobalOverlay,
            _ => throw new ArgumentOutOfRangeException(nameof(hook), hook, null),
        };

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
                    set(info.Feature, FeatureState.Failed, "no patch classes");
                    continue;
                }

                int applied = 0;

                try
                {
                    foreach (var type in classes)
                        applied += harmony.CreateClassProcessor(type).Patch().Count;

                    FaultInjection.ThrowIfInjected(FaultInjection.APPLY, info);

                    set(info.Feature, FeatureState.Active, null);
                    LoaderLog.Info($"  {info.Id}: active, {applied} method(s) patched");
                }
                catch (Exception e)
                {
                    int removed = rollback(harmony, classes);
                    set(info.Feature, FeatureState.Failed, describe(e));

                    // details only go to the log, the user gets one summary notification in report()
                    LoaderLog.Info($"  {info.Id}: failed after patching {applied} method(s), rolled back {removed} patch(es)");
                    LoaderLog.Info(e.ToString());
                }
            }

            report();
        }

        /// <summary>
        ///     runs patch code for a feature, if it throws the feature turns off
        /// </summary>
        public static void Run(Feature feature, Action action)
        {
            if (!IsActive(feature))
                return;

            try
            {
                FaultInjection.ThrowIfInjected(FaultInjection.RUNTIME, Info(feature));
                action();
            }
            catch (Exception e)
            {
                Fault(feature, e);
            }
        }

        /// <summary>
        ///     for patch code that can't go through <see cref="Run"/>
        /// </summary>
        public static void Fault(Feature feature, Exception exception)
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
        }

        private static Feature? feature(Type patchClass) =>
            (patchClass.GetCustomAttributes(typeof(FeatureAttribute), false).FirstOrDefault() as FeatureAttribute)?.Feature;

        // removes exactly the patches these classes added
        private static int rollback(Harmony harmony, List<Type> classes)
        {
            int removed = 0;

            foreach (var original in harmony.GetPatchedMethods().ToList())
            {
                var info = Harmony.GetPatchInfo(original);
                if (info == null)
                    continue;

                var ours = info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers)
                               .Where(p => p.owner == harmony.Id && classes.Contains(p.PatchMethod.DeclaringType!))
                               .ToList();

                foreach (var patch in ours)
                {
                    try
                    {
                        harmony.Unpatch(original, patch.PatchMethod);
                        removed++;
                    }
                    catch (Exception e)
                    {
                        LoaderLog.Error($"Couldn't roll back {patch.PatchMethod.DeclaringType?.Name}.{patch.PatchMethod.Name} on {original.Name}.", e);
                    }
                }
            }

            return removed;
        }

        private static void set(Feature feature, FeatureState state, string? reason)
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
