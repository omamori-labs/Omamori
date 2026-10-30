using System.Collections.Generic;

namespace Omamori.Core
{
    /// <summary>
    ///     omamori/settings.json, what the player turned off in the settings mod list, read once per launch
    /// </summary>
    internal sealed class ModSwitches
    {
        public const string FILENAME = "settings.json";

        private readonly string path;
        private readonly State state;
        private readonly object state_lock = new object();

        private ModSwitches(string path, State state)
        {
            this.path = path;
            this.state = state;
        }

        public static ModSwitches Open(string path) => new ModSwitches(path, JsonFile.Read<State>(path, "every mod stays on"));

        public bool IsEnabled(string id)
        {
            lock (state_lock)
                return !state.DisabledMods.Contains(id);
        }

        public void SetEnabled(string id, bool enabled)
        {
            lock (state_lock)
            {
                if (enabled == !state.DisabledMods.Contains(id))
                    return;

                if (enabled)
                    state.DisabledMods.RemoveAll(d => d == id);
                else
                    state.DisabledMods.Add(id);

                JsonFile.Write(path, state);
            }
        }

        private sealed class State
        {
            public List<string> DisabledMods { get; set; } = [];
        }
    }
}
