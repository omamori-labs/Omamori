using System;
using System.Collections.Generic;
using System.Globalization;
using Omamori.API;

namespace Omamori.NowPlaying
{
    public sealed class Status
    {
        public const string PLACEHOLDERS = "{artist} {title} {diff} {mapper} {stars} {bpm} {length} {mods}";

        public GameScreen Screen { get; set; }
        public BeatmapDetails? Beatmap { get; set; }
        public IReadOnlyList<string> Mods { get; set; } = [];
        public PlayDetails? Play { get; set; }

        public (BeatmapDetails Beatmap, IReadOnlyList<string> Mods)? Shown() => Screen switch
        {
            GameScreen.Gameplay when Play != null => (Play.Beatmap, Play.Mods),
            GameScreen.Results when Beatmap != null => (Beatmap, Mods),
            GameScreen.Editor when Beatmap != null => (Beatmap, []),
            _ => null,
        };

        public static string Fill(string format, BeatmapDetails beatmap, IReadOnlyList<string> mods) =>
            format.Replace("{artist}", beatmap.Artist, StringComparison.Ordinal)
                  .Replace("{title}", beatmap.Title, StringComparison.Ordinal)
                  .Replace("{diff}", beatmap.Difficulty, StringComparison.Ordinal)
                  .Replace("{mapper}", beatmap.Creator, StringComparison.Ordinal)
                  .Replace("{stars}", beatmap.StarRating < 0 ? "?" : beatmap.StarRating.ToString("0.00", CultureInfo.InvariantCulture), StringComparison.Ordinal)
                  .Replace("{bpm}", Math.Round(beatmap.Bpm).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                  .Replace("{length}", beatmap.Length.ToString(@"m\:ss", CultureInfo.InvariantCulture), StringComparison.Ordinal)
                  .Replace("{mods}", mods.Count == 0 ? string.Empty : "+" + string.Concat(mods), StringComparison.Ordinal)
                  .Trim();
    }
}
