using System;
using System.Collections.Generic;
using Omamori.API;

namespace Omamori.Core
{
    /// <summary>
    ///     omamori/faults.json, which mods crashed in which session
    ///     <see cref="STRIKES_TO_SKIP"/> crashes skips loading a mod until its version changes
    /// </summary>
    internal sealed class FaultHistory
    {
        public const string FILENAME = "faults.json";

        public const int STRIKES_TO_SKIP = 2;

        private readonly string path;
        private readonly State state;
        private readonly object state_lock = new object();

        public int Session => state.Session;

        private FaultHistory(string path, State state)
        {
            this.path = path;
            this.state = state;
        }

        public static FaultHistory Open(string path)
        {
            var state = JsonFile.Read<State>(path, "starting a new crash history");
            state.Session++;

            var history = new FaultHistory(path, state);
            history.save();
            return history;
        }

        public string? SkipReason(ModManifest manifest)
        {
            lock (state_lock)
            {
                if (!state.Mods.TryGetValue(manifest.Id, out var record))
                    return null;

                if (record.Version != manifest.Version)
                {
                    state.Mods.Remove(manifest.Id);
                    save();
                    return null;
                }

                return record.Strikes >= STRIKES_TO_SKIP
                    ? $"crashed in {record.Strikes} sessions in a row ({record.LastError}), it stays off until it updates or you try it again"
                    : null;
            }
        }

        // only meaningful after SkipReason ran for this launch, that drops records for older versions
        public bool Skips(string id)
        {
            lock (state_lock)
                return state.Mods.TryGetValue(id, out var record) && record.Strikes >= STRIKES_TO_SKIP;
        }

        /// <summary>the mod loads again next launch</summary>
        public void Forget(string id)
        {
            lock (state_lock)
            {
                if (state.Mods.Remove(id))
                    save();
            }
        }

        public int Record(ModManifest manifest, Exception exception)
        {
            lock (state_lock)
            {
                if (!state.Mods.TryGetValue(manifest.Id, out var record) || record.Version != manifest.Version)
                    state.Mods[manifest.Id] = record = new ModRecord { Version = manifest.Version };

                if (record.LastSession != state.Session)
                {
                    record.Strikes = record.LastSession == state.Session - 1 ? record.Strikes + 1 : 1;
                    record.LastSession = state.Session;
                }

                record.LastError = $"{exception.GetType().Name}: {exception.Message}";
                record.LastFault = DateTimeOffset.UtcNow;

                save();
                return record.Strikes;
            }
        }

        private void save() => JsonFile.Write(path, state);

        private sealed class State
        {
            public int Session { get; set; }

            public Dictionary<string, ModRecord> Mods { get; set; } = [];
        }

        private sealed class ModRecord
        {
            public string Version { get; set; } = string.Empty;

            public int Strikes { get; set; }

            public int LastSession { get; set; }

            public string LastError { get; set; } = string.Empty;

            public DateTimeOffset LastFault { get; set; }
        }
    }
}
