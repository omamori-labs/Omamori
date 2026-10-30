namespace Omamori.API
{
    public sealed record BeatmapSetDetails
    {
        public string Title { get; init; } = string.Empty;
        public string Artist { get; init; } = string.Empty;
        public string Creator { get; init; } = string.Empty;

        /// <summary>null for sets that aren't on the website</summary>
        public int? OnlineId { get; init; }

        public int Difficulties { get; init; }
    }
}
