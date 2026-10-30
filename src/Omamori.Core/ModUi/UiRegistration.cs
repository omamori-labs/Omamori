using System;
using osu.Framework.Graphics;

namespace Omamori.Core.ModUi
{
    internal sealed record UiRegistration(string ModId, Func<Drawable> Factory);
}
