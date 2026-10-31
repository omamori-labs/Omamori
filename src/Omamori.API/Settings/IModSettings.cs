using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Graphics;

namespace Omamori.API
{
    /// <summary>
    ///     declare in <see cref="IMod.Load"/>, the player changes them in the mod manager's settings tab
    ///     the bindable you get back changes live on the update thread, unless the setting <see cref="SettingOptions.RequiresRestart"/>
    ///     values are saved per mod and survive updates, a stored value that no longer fits falls back to the default
    /// </summary>
    public interface IModSettings
    {
        IBindable<bool> Toggle(string key, string label, bool defaultValue, SettingOptions? options = null);

        /// <summary>step is the smallest change, 1 for whole numbers</summary>
        IBindable<double> Number(string key, string label, double defaultValue, double min, double max, double step = 0.01, SettingOptions? options = null);

        IBindable<string> Text(string key, string label, string defaultValue = "", SettingOptions? options = null);

        IBindable<T> Choice<T>(string key, string label, T defaultValue, SettingOptions? options = null)
            where T : struct, Enum;

        /// <summary>a dropdown of strings, for choices only known at runtime</summary>
        IBindable<string> Choice(string key, string label, string defaultValue, IReadOnlyList<string> choices, SettingOptions? options = null);

        IBindable<int> WholeNumber(string key, string label, int defaultValue, int min, int max, SettingOptions? options = null);

        IBindable<Colour4> Colour(string key, string label, Colour4 defaultValue, SettingOptions? options = null);

        /// <summary>a full path, empty when nothing is picked, extensions like ".png", all files when none are given</summary>
        IBindable<string> File(string key, string label, IReadOnlyList<string>? extensions = null, SettingOptions? options = null);

        /// <summary>shown as dots, still saved as plain text in omamori/config, fine for an api token, not for a real password</summary>
        IBindable<string> Secret(string key, string label, SettingOptions? options = null);

        /// <summary>keys the player can rebind, listen for them with <see cref="IModContext.Input"/>, <see cref="ModKeys.NONE"/> is unbound</summary>
        IBindable<ModKeys> KeyBinding(string key, string label, ModKeys defaultKeys, SettingOptions? options = null);

        /// <summary>runs <paramref name="onClick"/> on the update thread, a throw turns the mod off like any other</summary>
        void Button(string label, string buttonText, Action onClick);

        /// <summary>starts a group of settings</summary>
        void Header(string text);

        /// <summary>a line of text between settings</summary>
        void Note(string text);
    }
}
