namespace Omamori.API
{
    /// <summary>
    ///     what a mod can do beyond the api, set in its manifest
    ///     only capabilities the loader enforces belong here, what a mod uses of the api is said by its hooks
    /// </summary>
    public enum ModCapability
    {
        /// <summary>changes osu!'s own code with harmony, required for mods built with OmamoriHarmony</summary>
        Patches = 2,
    }
}
