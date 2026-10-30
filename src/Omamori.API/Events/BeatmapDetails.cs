using System;

namespace Omamori.API
{
    public sealed record BeatmapDetails
    {
        public string Title { get; init; } = string.Empty;
        public string Artist { get; init; } = string.Empty;

        /// <summary>the mapper</summary>
        public string Creator { get; init; } = string.Empty;

        public string Difficulty { get; init; } = string.Empty;

        /// <summary>-1 when it hasn't been calculated yet</summary>
        public double StarRating { get; init; }

        public double Bpm { get; init; }
        public TimeSpan Length { get; init; }

        /// <summary>null for beatmaps that aren't on the website</summary>
        public int? OnlineId { get; init; }

        public string Ruleset { get; init; } = string.Empty;
    }
}
