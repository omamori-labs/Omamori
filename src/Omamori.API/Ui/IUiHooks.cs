using System;
using osu.Framework.Graphics;

namespace Omamori.API
{
    /// <summary>
    ///     mod ui inj, called when target is created
    /// </summary>
    public interface IUiHooks
    {
        void AddToMainMenu(Func<Drawable> factory);

        void AddGlobalOverlay(Func<Drawable> factory);
    }
}
