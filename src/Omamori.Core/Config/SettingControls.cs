using System;
using System.Collections.Generic;
using System.IO;
using Omamori.API;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;

namespace Omamori.Core.Config
{
    /// <summary>
    ///     the lazer control each kind of setting shows as in the mod manager
    /// </summary>
    internal static class SettingControls
    {
        public static FormCheckBox Toggle(ConfigSetting setting, Bindable<bool> current) =>
            new FormCheckBox { Caption = setting.Label, HintText = setting.Hint, Current = current };

        public static FormSliderBar<double> Number(ConfigSetting setting, Bindable<double> current) => new FormSliderBar<double>
        {
            Caption = setting.Label,
            HintText = setting.Hint,
            DisplayAsPercentage = setting.Options.Percentage,
            Current = current,
        };

        public static FormSliderBar<int> WholeNumber(ConfigSetting setting, Bindable<int> current) =>
            new FormSliderBar<int> { Caption = setting.Label, HintText = setting.Hint, Current = current };

        public static FormTextBox Text(ConfigSetting setting, Bindable<string> current) =>
            new FormTextBox { Caption = setting.Label, HintText = setting.Hint, Current = current };

        public static FormPasswordTextBox Secret(ConfigSetting setting, Bindable<string> current) =>
            new FormPasswordTextBox { Caption = setting.Label, HintText = setting.Hint, Current = current };

        public static FormEnumDropdown<T> Choice<T>(ConfigSetting setting, Bindable<T> current)
            where T : struct, Enum =>
            new FormEnumDropdown<T> { Caption = setting.Label, HintText = setting.Hint, Current = current };

        public static FormDropdown<string> Choice(ConfigSetting setting, Bindable<string> current, IReadOnlyList<string> choices) =>
            new FormDropdown<string> { Caption = setting.Label, HintText = setting.Hint, Items = choices, Current = current };

        public static SettingsColour Colour(ConfigSetting setting, Bindable<Colour4> current) =>
            new SettingsColour { LabelText = setting.Label, TooltipText = setting.Hint, Current = current };

        public static KeyBindingControl KeyBinding(string modId, ConfigSetting setting, Bindable<ModKeys> current) =>
            new KeyBindingControl(modId, setting.Key, setting.Label, current) { TooltipText = setting.Hint };

        // the selector works in FileInfo, mods get a plain path
        public static FormFileSelector File(ConfigSetting setting, Bindable<string> path, string[] extensions)
        {
            var selector = new FormFileSelector(extensions) { Caption = setting.Label, HintText = setting.Hint, AllowClear = true };

            selector.Current.Value = path.Value.Length > 0 ? new FileInfo(path.Value) : null;
            selector.Current.BindValueChanged(e => path.Value = e.NewValue?.FullName ?? string.Empty);
            path.BindValueChanged(e =>
            {
                if (e.NewValue != (selector.Current.Value?.FullName ?? string.Empty))
                    selector.Current.Value = e.NewValue.Length > 0 ? new FileInfo(e.NewValue) : null;
            });

            return selector;
        }
    }
}
