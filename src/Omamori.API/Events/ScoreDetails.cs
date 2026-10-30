using System;
using System.Collections.Generic;

namespace Omamori.API
{
    public sealed record ScoreDetails
    {
        public long TotalScore { get; init; }

        /// <summary>0 to 1</summary>
        public double Accuracy { get; init; }

        /// <summary>D, C, B, A, S, SH, X, XH, or F for a fail</summary>
        public string Rank { get; init; } = string.Empty;

        public int MaxCombo { get; init; }

        /// <summary>null until the server works it out, or when it doesn't give pp</summary>
        public double? Pp { get; init; }

        public bool Passed { get; init; }

        public DateTimeOffset Date { get; init; }

        public string Ruleset { get; init; } = string.Empty;

        public IReadOnlyList<string> Mods { get; init; } = [];

        /// <summary>null when the beatmap isn't in the library</summary>
        public BeatmapDetails? Beatmap { get; init; }
    }
}
