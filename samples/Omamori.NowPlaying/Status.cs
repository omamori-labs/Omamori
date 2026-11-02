using System;
using System.Collections.Generic;
using System.Globalization;
using Omamori.API;

namespace Omamori.NowPlaying
{
    public sealed class Status
    {
        public const string DEFAULT_FORMAT = "{artist} - {title} [{diff}] {mods}";
        public const string PLACEHOLDERS = "{artist} {title} {diff} {mapper} {stars} {bpm} {mods}";

        public GameScreen Screen { get; set; }
        public BeatmapDetails? Beatmap { get; set; }
        public IReadOnlyList<string> Mods { get; set; } = [];
        public PlayDetails? Play { get; set; }
        public string Format { get; set; } = DEFAULT_FORMAT;

        public string Describe() => Screen switch
        {
            GameScreen.Gameplay when Play != null => $"{(Play.IsReplay ? "Watching" : "Playing")} {fill(Play.Beatmap, Play.Mods)}",
            GameScreen.SongSelect => "Picking a song",
            GameScreen.Results when Beatmap != null => $"Looking at results for {fill(Beatmap, Mods)}",
            GameScreen.Editor when Beatmap != null => $"Editing {fill(Beatmap, [])}",
            GameScreen.Multiplayer => "In multiplayer",
            GameScreen.Playlists => "In playlists",
            _ => "In the menus",
        };

        private string fill(BeatmapDetails beatmap, IReadOnlyList<string> mods) =>
            Format.Replace("{artist}", beatmap.Artist, StringComparison.Ordinal)
                  .Replace("{title}", beatmap.Title, StringComparison.Ordinal)
                  .Replace("{diff}", beatmap.Difficulty, StringComparison.Ordinal)
                  .Replace("{mapper}", beatmap.Creator, StringComparison.Ordinal)
                  .Replace("{stars}", beatmap.StarRating < 0 ? "?" : beatmap.StarRating.ToString("0.00", CultureInfo.InvariantCulture), StringComparison.Ordinal)
                  .Replace("{bpm}", Math.Round(beatmap.Bpm).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                  .Replace("{mods}", mods.Count == 0 ? string.Empty : "+" + string.Concat(mods), StringComparison.Ordinal)
                  .Trim();
    }
}
