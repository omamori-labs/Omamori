namespace Omamori.Core.Features
{
    internal sealed record FeatureStatus(FeatureInfo Info, FeatureState State, string? Reason);
}
