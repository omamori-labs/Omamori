namespace Omamori.API
{
    public sealed record RulesetDetails
    {
        /// <summary>osu, taiko, fruits, mania, or a custom ruleset's short name</summary>
        public string ShortName { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;
    }
}
