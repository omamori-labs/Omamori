using osu.Framework.Graphics;

namespace Omamori.Core.Config
{
    internal abstract class ConfigElement
    {
        /// <summary>restart only and changed since launch</summary>
        public virtual bool Pending => false;

        public abstract Drawable CreateItem();
    }
}
