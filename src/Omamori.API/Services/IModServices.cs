namespace Omamori.API
{
    public interface IModServices
    {
        /// <summary>from <see cref="IMod.Load"/>, other mods find it by <typeparamref name="T"/>, throws if this mod already provides one</summary>
        void Provide<T>(T service)
            where T : class;

        /// <summary>
        ///     null when nothing provides it or the provider was turned off, the first provider wins if there are several
        ///     declare the provider as a dependency so it loads first, and reference its dll with OmamoriModReference instead of bundling it
        /// </summary>
        T? Get<T>()
            where T : class;
    }
}
