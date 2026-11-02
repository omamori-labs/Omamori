namespace Omamori.API
{
    /// <summary>
    ///     null keeps what osu! would show
    /// </summary>
    public sealed record PresenceStatus
    {
        /// <summary>the top line</summary>
        public string? Details { get; init; }

        /// <summary>the line under <see cref="Details"/></summary>
        public string? State { get; init; }

        /// <summary>a https url or one of osu!'s own image keys like osu_logo_lazer, at most 256 bytes</summary>
        public string? LargeImage { get; init; }

        /// <summary>shown when hovering the large image</summary>
        public string? LargeImageText { get; init; }

        /// <summary>the corner icon same rules as <see cref="LargeImage"/></summary>
        public string? SmallImage { get; init; }

        /// <summary>shown when hovering the small image</summary>
        public string? SmallImageText { get; init; }
    }
}
