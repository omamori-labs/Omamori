namespace Omamori.Core
{
    internal sealed record FeatureStatus(FeatureInfo Info, FeatureState State, string? Reason);
}
