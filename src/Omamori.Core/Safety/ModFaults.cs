using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Omamori.Core.Loading;
using Omamori.Core.ModUi;
using osu.Framework.Threading;

namespace Omamori.Core.Safety
{
    /// <summary>
    ///     turns off a mod that throws an unhandled exception, removing its ui and patches
    /// </summary>
    internal static class ModFaults
    {
        internal static readonly TimeSpan GRACE = TimeSpan.FromSeconds(2);

        private static readonly Dictionary<string, DateTime> faulted = [];
        private static readonly object fault_lock = new object();

        public static FaultHistory? History { get; set; }

        /// <summary>the mod's id, raised on whichever thread it threw</summary>
        public static event Action<string>? Faulted;

        public static bool IsFaulted(string id)
        {
            lock (fault_lock)
                return faulted.ContainsKey(id);
        }

        /// <summary>
        ///     runs a mod's own code from omamori's, a throw turns that mod off instead of whatever called it, update thread only
        /// </summary>
        public static void RunForMod(string modId, string what, Action action)
        {
            if (IsFaulted(modId))
                return;

            try
            {
                action();
            }
            catch (Exception e)
            {
                if (ModLoader.Get(modId) is { } mod)
                    Contain(mod, e, scheduler: null);
                else
                    LoaderLog.Error($"[{modId}] {what} threw.", e);
            }
        }

        // the exception can come from any thread, cleanup runs on the update thread
        public static bool TryContain(Exception exception, Scheduler? scheduler) =>
            Blame(exception, modOf) is { } mod && Contain(mod, exception, scheduler);

        // a null scheduler cleans up right away, only from the update thread
        public static bool Contain(LoadedMod mod, Exception exception, Scheduler? scheduler)
        {
            string id = mod.Manifest.Id;

            lock (fault_lock)
            {
                if (faulted.TryGetValue(id, out var since))
                    return DateTime.UtcNow - since < GRACE;

                faulted[id] = DateTime.UtcNow;
            }

            int strikes = History?.Record(mod.Manifest, exception) ?? 1;

            LoaderLog.Info($"[{id}] threw {exception.GetType().Name}: {exception.Message}, turning it off for this session.");
            LoaderLog.Notify(strikes >= FaultHistory.STRIKES_TO_SKIP
                ? $"{mod.Manifest.Name} crashed again and was turned off. It won't load next time until it updates."
                : $"{mod.Manifest.Name} crashed and was turned off for this session, osu! kept running.");

            if (scheduler != null)
                scheduler.Add(() => cleanUp(mod));
            else
                cleanUp(mod);

            Faulted?.Invoke(id);
            return true;
        }

        // throw site first, so the mod that threw is blamed rather than one that called it
        internal static T? Blame<T>(Exception? exception, Func<Assembly, T?> owner)
            where T : class
        {
            foreach (var frame in CrashAttribution.Frames(exception))
            {
                if (frame.GetMethod()?.DeclaringType?.Assembly is { } assembly && owner(assembly) is { } found)
                    return found;
            }

            return null;
        }

        private static LoadedMod? modOf(Assembly assembly)
        {
            var context = AssemblyLoadContext.GetLoadContext(assembly);

            return context == null || context == AssemblyLoadContext.Default
                ? null
                : ModLoader.LOADED.ToList().FirstOrDefault(m => m.Loader.LoadContext == context);
        }

        private static void cleanUp(LoadedMod mod)
        {
            int removed = UiHooks.Remove(mod.Manifest.Id);
            int unpatched = PatchRemoval.RemoveFrom(mod.Loader.LoadContext);

            LoaderLog.Info($"[{mod.Manifest.Id}] removed {removed} drawable(s) and {unpatched} patch(es).");
        }
    }
}
