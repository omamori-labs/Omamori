namespace Omamori.API
{
    public sealed record WindowDetails
    {
        public bool Focused { get; init; }
        public bool Fullscreen { get; init; }
    }
}
