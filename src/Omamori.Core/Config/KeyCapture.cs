using System.Linq;
using Omamori.API;
using osu.Framework.Extensions;
using osu.Framework.Input.Bindings;

namespace Omamori.Core.Config
{
    internal static class KeyCapture
    {
        public static KeyCaptureResult Decide(InputKey key, KeyCombination pressed)
        {
            bool alone = pressed.Keys.Count(k => !KeyCombination.IsModifierKey(k)) == 1 && !pressed.Keys.Any(KeyCombination.IsModifierKey);

            if (key == InputKey.Escape && alone)
                return KeyCaptureResult.Cancel;

            if (key is InputKey.Delete or InputKey.BackSpace && alone)
                return KeyCaptureResult.Clear;

            return KeyCombination.IsModifierKey(key) ? KeyCaptureResult.Waiting : KeyCaptureResult.Bind;
        }

        /// <summary>
        ///     the held modifiers and the key that was pressed
        /// </summary>
        public static ModKeys Combination(KeyCombination pressed, InputKey trigger) =>
            Normalise(new ModKeys([.. pressed.Keys.Where(KeyCombination.IsModifierKey).Append(trigger)]));

        public static ModKeys Normalise(ModKeys keys) => new ModKeys([.. keys.Keys.Select(k => k.GetVirtualKey() ?? k)]);
    }
}
