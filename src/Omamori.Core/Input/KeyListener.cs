using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;

namespace Omamori.Core.Input
{
    /// <summary>
    ///     sits on top of the game so it hears keys before the screens, never handles them so they carry on
    /// </summary>
    internal sealed partial class KeyListener : Drawable
    {
        public KeyListener()
        {
            RelativeSizeAxes = Axes.Both;
            AlwaysPresent = true;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!e.Repeat)
                ModInput.KeysChanged(KeyCombination.FromInputState(e.CurrentState), e.CurrentState);

            return false;
        }

        protected override void OnKeyUp(KeyUpEvent e) => ModInput.KeysChanged(KeyCombination.FromInputState(e.CurrentState), e.CurrentState);
    }
}
