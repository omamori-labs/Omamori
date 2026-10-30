using System;
using System.Collections.Generic;
using Omamori.API;
using Omamori.Core.Safety;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;

namespace Omamori.Core.Config
{
    /// <summary>
    ///     the player edits stored, the mod reads live, they only differ for restart only settings
    /// </summary>
    internal sealed class ConfigSetting<T> : ConfigSetting
    {
        private readonly T launchValue;
        private readonly Func<ConfigSetting, Bindable<T>, Drawable> control;

        public readonly Bindable<T> Stored;
        public readonly Bindable<T> Live;

        public override bool Pending => Options.RequiresRestart && !EqualityComparer<T>.Default.Equals(Stored.Value, launchValue);

        public ConfigSetting(string modId, string key, string label, SettingOptions options, Func<T, Bindable<T>> create, T defaultValue, T initial,
                          Func<ConfigSetting, Bindable<T>, Drawable> control, Action<T> save)
            : base(key, label, options)
        {
            this.control = control;
            launchValue = initial;

            Stored = create(defaultValue);
            Stored.Value = initial;
            Live = create(defaultValue);
            Live.Value = initial;

            Stored.BindValueChanged(e =>
            {
                save(e.NewValue);

                if (Options.RequiresRestart)
                    ModConfig.RaisePendingChanged();
                else
                    ModFaults.RunForMod(modId, $"setting {Key} changed handler", () => Live.Value = e.NewValue);
            });
        }

        public override Drawable CreateItem()
        {
            var current = Stored.GetBoundCopy();
            var drawable = control(this, current);
            var note = new Bindable<SettingsNote.Data?>();

            current.BindValueChanged(_ => note.Value = Pending ? new SettingsNote.Data("Applies next time osu! starts.", SettingsNote.Type.Informational) : null, true);

            // form controls get the settings item, older controls like the colour picker already are one
            if (drawable is IFormControl form)
            {
                var item = new SettingsItemV2(form);
                item.Note.BindTo(note);
                return item;
            }

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Children =
                [
                    drawable is SettingsItem<T> ? drawable : SettingsLayout.Padded(drawable),
                    SettingsLayout.Padded(new SettingsNote { RelativeSizeAxes = Axes.X, Current = { BindTarget = note } }),
                ],
            };
        }
    }
}
