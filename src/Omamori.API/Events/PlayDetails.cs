using System.Collections.Generic;

namespace Omamori.API
{
    public sealed record PlayDetails
    {
        public BeatmapDetails Beatmap { get; init; } = new BeatmapDetails();

        public string Ruleset { get; init; } = string.Empty;

        /// <summary>acronyms like HD, DT</summary>
        public IReadOnlyList<string> Mods { get; init; } = [];

        /// <summary>watching a replay, autoplay or spectating, not the player playing</summary>
        public bool IsReplay { get; init; }
    }
}
