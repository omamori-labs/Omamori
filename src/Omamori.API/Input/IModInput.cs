using System;
using osu.Framework.Bindables;

namespace Omamori.API
{
    public interface IModInput
    {
        /// <summary>
        ///     pass the bindable from <see cref="IModSettings.KeyBinding"/>
        /// </summary>
        void OnPressed(IBindable<ModKeys> keys, Action handler);

        void OnReleased(IBindable<ModKeys> keys, Action handler);
    }
}
