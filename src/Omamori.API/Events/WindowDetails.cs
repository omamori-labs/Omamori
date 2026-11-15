namespace Omamori.API
{
    public sealed record WindowDetails
    {
        public bool IsFocused { get; init; }
        public bool IsFullscreen { get; init; }
    }
}
