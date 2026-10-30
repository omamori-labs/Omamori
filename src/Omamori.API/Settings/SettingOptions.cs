namespace Omamori.API
{
    public sealed record SettingOptions
    {
        /// <summary>shown on hover</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>the change is saved but only reaches the mod next launch, for things only read in Load</summary>
        public bool RequiresRestart { get; init; }

        /// <summary>sliders only, shows 0.5 as 50%</summary>
        public bool Percentage { get; init; }
    }
}
