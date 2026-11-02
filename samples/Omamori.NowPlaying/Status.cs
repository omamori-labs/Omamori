using Omamori.API;

namespace Omamori.NowPlaying
{
    public sealed class Status
    {
        public GameScreen Screen { get; set; }
        public BeatmapDetails? Beatmap { get; set; }
        public PlayDetails? Play { get; set; }

        public string Describe() => Screen switch
        {
            GameScreen.Gameplay when Play != null => $"{(Play.IsReplay ? "Watching" : "Playing")} {name(Play.Beatmap)}",
            GameScreen.SongSelect => "Picking a song",
            GameScreen.Results when Beatmap != null => $"Looking at results for {name(Beatmap)}",
            GameScreen.Editor when Beatmap != null => $"Editing {name(Beatmap)}",
            GameScreen.Multiplayer => "In multiplayer",
            GameScreen.Playlists => "In playlists",
            _ => "In the menus",
        };

        private static string name(BeatmapDetails beatmap) => $"{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}]";
    }
}
