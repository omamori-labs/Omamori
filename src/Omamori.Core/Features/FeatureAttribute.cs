using System;

namespace Omamori.Core
{
    /// <summary>
    ///     marks a harmony patch class as part of a feature
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class FeatureAttribute(Feature feature) : Attribute
    {
        public Feature Feature { get; } = feature;
    }
}
