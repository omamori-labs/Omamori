using Omamori.Core.Features;

namespace Omamori.Core.Patching
{
    /// <summary>
    ///     the only code that hooks into lazer, one file per area
    ///     patch methods are marked [HarmonyPrefix]/[HarmonyPostfix] instead of relying on harmonys name convention, so they can be camelCase like every private method
    ///     every patch class needs a <see cref="FeatureAttribute"/>, and its code goes through <see cref="FeatureRegistry"/> so a throw turns the feature off
    /// </summary>
    internal static partial class Patches
    {
    }
}
