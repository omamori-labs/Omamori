using Omamori.API;

namespace Omamori.Core
{
    /// <param name="Feature">the feature this describes</param>
    /// <param name="Id">stable kebab-case id for logs, fault injection and the manager</param>
    /// <param name="Name">shown to users</param>
    /// <param name="Impact">what stops working when it's off, shown to users</param>
    /// <param name="Hook">the public hook this feature backs</param>
    internal sealed record FeatureInfo(Feature Feature, string Id, string Name, string Impact, Hook? Hook = null);
}
