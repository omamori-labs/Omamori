using System;
using osu.Framework.Bindables;

namespace Omamori.API
{
    /// <summary>
    ///     handlers run on the update thread, during plays too, and never stop the key reaching the game
    /// </summary>
    public interface IModInput
    {
        /// <summary>
        ///     pass the bindable from <see cref="IModSettings.KeyBinding"/>, follows the player rebinding it
        /// </summary>
        void OnPressed(IBindable<ModKeys> keys, Action handler);

        /// <summary>
        ///     when the combination stops being held, after <see cref="OnPressed"/>
        /// </summary>
        void OnReleased(IBindable<ModKeys> keys, Action handler);
    }
}
