using System;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays.Toolbar;

namespace Omamori.Core.ModUi
{
    /// <summary>
    ///     lazer's own toolbar button, so hover, tooltips and flashing match the buttons around it
    /// </summary>
    internal sealed partial class ModToolbarButton : ToolbarButton
    {
        private readonly IconUsage icon;

        public ModToolbarButton(IconUsage icon, string tooltip, string? subTooltip, Action action)
        {
            this.icon = icon;

            TooltipMain = tooltip;
            TooltipSub = subTooltip ?? string.Empty;
            Action = action;
        }

        // the icon container only exists once the button has loaded
        protected override void LoadComplete()
        {
            base.LoadComplete();
            SetIcon(icon);
        }
    }
}
