namespace Omamori.API
{
    /// <summary>
    ///     creates an instance per mod, calls <see cref="Load"/> once at game launch
    /// </summary>
    public interface IMod
    {
        void Load(IModContext context);
    }
}
