namespace Omamori.API
{
    /// <summary>
    ///     the same frames a .osr file holds, X and Y are playfield positions in osu!, other rulesets pack their own data into them
    /// </summary>
    public sealed record ReplayFrameDetails
    {
        public double Time { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public ReplayButtons Buttons { get; init; }
    }
}
