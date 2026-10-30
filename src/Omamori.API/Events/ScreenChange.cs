namespace Omamori.API
{
    public sealed record ScreenChange
    {
        public GameScreen Screen { get; init; }

        public GameScreen Previous { get; init; }

        /// <summary>lazer's own class name, not stable across osu! versions, for logging</summary>
        public string ScreenName { get; init; } = string.Empty;
    }
}
