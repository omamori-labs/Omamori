namespace Omamori.API
{
    public sealed class ModDependency
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>lowest accepted version, any version if empty</summary>
        public string MinVersion { get; set; } = string.Empty;

        /// <summary>
        ///     required deps that are missing or fail skip this mod
        ///     optional ones just load first when present, check with <see cref="IModContext.IsModLoaded"/>
        /// </summary>
        public bool Optional { get; set; }
    }
}
