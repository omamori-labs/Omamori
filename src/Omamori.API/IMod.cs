namespace Omamori.API
{
    /// <summary>
    ///     creates an instance per mod, calls <see cref="Load"/> once at game launch
    /// </summary>
    public interface IMod
    {
        void Load(IModContext context);

        /// <summary>
        ///     the player turned the mod off in game, runs on the update thread before omamori takes it out of the session
        ///     omamori removes everything registered through <see cref="IModContext"/> and the mod's patches itself
        ///     anything else it started, like threads, timers or open files, should stop here
        ///     turning it back on waits for the next launch, so <see cref="Load"/> isn't called again this session
        /// </summary>
        void Unload()
        {
        }
    }
}
