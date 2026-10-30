using Omamori.API;
using osu.Framework.Localisation;

namespace Omamori.Core
{
    internal abstract class ConfigSetting(string key, string label, SettingOptions options) : ConfigElement
    {
        public string Key { get; } = key;
        public string Label { get; } = label;
        public SettingOptions Options { get; } = options;

        // lazer shows the hint icon for any non-default string, even an empty one
        public LocalisableString Hint => Options.Description.Length > 0 ? Options.Description : default(LocalisableString);
    }
}
