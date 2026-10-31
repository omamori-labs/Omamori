namespace Omamori.API
{
    public interface IModServices
    {
        /// <summary>from <see cref="IMod.Load"/>, other mods find it by <typeparamref name="T"/></summary>
        void Provide<T>(T service)
            where T : class;

        T? Get<T>()
            where T : class;
    }
}
