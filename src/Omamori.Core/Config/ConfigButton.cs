using System;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterfaceV2;

namespace Omamori.Core
{
    internal sealed class ConfigButton(string modId, string label, string buttonText, Action onClick) : ConfigElement
    {
        public override Drawable CreateItem() => SettingsLayout.Padded(new FormButton
        {
            Caption = label,
            ButtonText = buttonText,
            Action = () => ModFaults.RunForMod(modId, $"{label} button", onClick),
        });
    }
}
