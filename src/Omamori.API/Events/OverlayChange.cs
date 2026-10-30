namespace Omamori.API
{
    public sealed record OverlayChange
    {
        public GameOverlay Overlay { get; init; }
        public bool Visible { get; init; }
    }
}
