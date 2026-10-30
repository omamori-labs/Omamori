namespace Omamori.Core.Features
{
    internal enum FeatureState
    {
        /// <summary>not applied yet</summary>
        Pending,

        Active,

        /// <summary>patching failed on startup, nothing of it is applied</summary>
        Failed,

        /// <summary>was active, its code threw while the game ran so it's off for the session</summary>
        Faulted,
    }
}
