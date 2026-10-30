using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
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

        private static readonly JsonSerializerOptions json_options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

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
            var state = new State();

            try
            {
                if (File.Exists(path))
                    state = JsonSerializer.Deserialize<State>(File.ReadAllText(path), json_options) ?? new State();
            }
            catch (Exception e)
            {
                LoaderLog.Error($"Couldn't read {path}, starting a new crash history.", e);
            }

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
                    ? $"crashed in {record.Strikes} sessions in a row ({record.LastError}), it stays off until it updates. Remove it from omamori/{FILENAME} to try again"
                    : null;
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

        private void save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(state, json_options));
                File.Move(temporary, path, overwrite: true);
            }
            catch (Exception e)
            {
                LoaderLog.Error($"Couldn't save {path}.", e);
            }
        }

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
