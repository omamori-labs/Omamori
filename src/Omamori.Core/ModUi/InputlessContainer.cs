using osu.Framework.Graphics.Containers;

namespace Omamori.Core.ModUi
{
    /// <summary>
    ///     nothing inside gets input, so mod UI shown during a play can't eat a tap meant for gameplay
    /// </summary>
    internal sealed partial class InputlessContainer : Container
    {
        public override bool PropagatePositionalInputSubTree => false;

        public override bool PropagateNonPositionalInputSubTree => false;
    }
}
