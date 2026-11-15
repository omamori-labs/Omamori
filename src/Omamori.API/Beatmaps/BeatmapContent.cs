using System.Collections.Generic;

namespace Omamori.API
{
    public sealed record BeatmapContent
    {
        public BeatmapDetails Beatmap { get; init; } = new BeatmapDetails();

        /// <summary>the ruleset it was converted for, which can differ from <see cref="BeatmapDetails.Ruleset"/> for a converted osu! map</summary>
        public string Ruleset { get; init; } = string.Empty;

        /// <summary>the mods applied</summary>
        public IReadOnlyList<ModDetails> Mods { get; init; } = [];

        /// <summary>in time order, after the mods have changed them</summary>
        public IReadOnlyList<HitObjectDetails> HitObjects { get; init; } = [];

        public IReadOnlyList<TimingPointDetails> TimingPoints { get; init; } = [];

        public IReadOnlyList<BreakDetails> Breaks { get; init; } = [];
    }
}
