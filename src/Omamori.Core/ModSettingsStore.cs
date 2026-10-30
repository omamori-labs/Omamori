using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Omamori.API;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;

namespace Omamori.Core
{
    /// <summary>
    ///     one per mod, values live in omamori/config/[id].json so they survive the mod updating
    ///     keys the mod no longer declares are kept, a mod update might bring them back
    /// </summary>
    internal sealed class ModSettingsStore : IModSettings, IDisposable
    {
        internal static readonly TimeSpan SAVE_DELAY = TimeSpan.FromMilliseconds(500);

        private static readonly Dictionary<string, ModSettingsStore> stores = [];

        /// <summary>a restart only setting changed, raised on the update thread</summary>
        public static event Action? PendingChanged;

        private readonly ModManifest manifest;
        private readonly string path;
        private readonly Dictionary<string, JsonElement> values;
        private readonly List<ModSettingElement> declared = [];
        private readonly Timer saveTimer;

        /// <summary>settings and layout, in the order the mod declared them</summary>
        public IReadOnlyList<ModSettingElement> Declared => declared;

        public ModSettingsStore(ModManifest manifest, string path)
        {
            this.manifest = manifest;
            this.path = path;

            values = JsonFile.Read<Dictionary<string, JsonElement>>(path, "every setting starts at its default");
            saveTimer = new Timer(_ => Flush());

            lock (stores)
                stores[manifest.Id] = this;
        }

        public static ModSettingsStore? For(string id)
        {
            lock (stores)
                return stores.GetValueOrDefault(id);
        }

        public static int PendingCount()
        {
            lock (stores)
                return stores.Values.Sum(s => s.declared.Count(d => d.Pending));
        }

        public static void FlushAll()
        {
            List<ModSettingsStore> all;

            lock (stores)
                all = [.. stores.Values];

            all.ForEach(s => s.Flush());
        }

        internal static void Clear()
        {
            lock (stores)
                stores.Clear();
        }

        public IBindable<bool> Toggle(string key, string label, bool defaultValue, SettingOptions? options = null) =>
            add(key, label, options, defaultValue, v => new BindableBool(v),
                e => e.ValueKind is JsonValueKind.True or JsonValueKind.False ? e.GetBoolean() : null,
                (setting, current) => new FormCheckBox { Caption = setting.Label, HintText = setting.Hint, Current = current });

        public IBindable<double> Number(string key, string label, double defaultValue, double min, double max, double step = 0.01, SettingOptions? options = null)
        {
            if (min > max || step <= 0 || defaultValue < min || defaultValue > max)
                throw new ArgumentException($"setting {key} needs min <= default <= max and a positive step");

            return add(key, label, options, defaultValue, v => new BindableDouble(v) { MinValue = min, MaxValue = max, Precision = step },
                e => e.ValueKind == JsonValueKind.Number && e.GetDouble() is var d && d >= min && d <= max ? d : null,
                (setting, current) => new FormSliderBar<double>
                {
                    Caption = setting.Label,
                    HintText = setting.Hint,
                    DisplayAsPercentage = setting.Options.Percentage,
                    Current = current,
                });
        }

        public IBindable<int> WholeNumber(string key, string label, int defaultValue, int min, int max, SettingOptions? options = null)
        {
            if (min > max || defaultValue < min || defaultValue > max)
                throw new ArgumentException($"setting {key} needs min <= default <= max");

            return add(key, label, options, defaultValue, v => new BindableInt(v) { MinValue = min, MaxValue = max },
                e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out int i) && i >= min && i <= max ? i : null,
                (setting, current) => new FormSliderBar<int> { Caption = setting.Label, HintText = setting.Hint, Current = current });
        }

        public IBindable<string> Text(string key, string label, string defaultValue = "", SettingOptions? options = null) =>
            add(key, label, options, defaultValue, v => new Bindable<string>(v), readString,
                (setting, current) => new FormTextBox { Caption = setting.Label, HintText = setting.Hint, Current = current });

        public IBindable<string> Secret(string key, string label, SettingOptions? options = null) =>
            add(key, label, options, string.Empty, v => new Bindable<string>(v), readString,
                (setting, current) => new FormPasswordTextBox { Caption = setting.Label, HintText = setting.Hint, Current = current });

        public IBindable<T> Choice<T>(string key, string label, T defaultValue, SettingOptions? options = null)
            where T : struct, Enum =>
            add(key, label, options, defaultValue, v => new Bindable<T>(v),
                e => e.ValueKind == JsonValueKind.String && Enum.TryParse<T>(e.GetString(), out var parsed) && Enum.IsDefined(parsed) ? parsed : null,
                (setting, current) => new FormEnumDropdown<T> { Caption = setting.Label, HintText = setting.Hint, Current = current },
                v => v.ToString());

        public IBindable<string> Choice(string key, string label, string defaultValue, IReadOnlyList<string> choices, SettingOptions? options = null)
        {
            if (!choices.Contains(defaultValue))
                throw new ArgumentException($"setting {key}'s default isn't one of its choices");

            return add(key, label, options, defaultValue, v => new Bindable<string>(v),
                e => readString(e) is string s && choices.Contains(s) ? s : null,
                (setting, current) => new FormDropdown<string> { Caption = setting.Label, HintText = setting.Hint, Items = choices, Current = current });
        }

        public IBindable<Colour4> Colour(string key, string label, Colour4 defaultValue, SettingOptions? options = null) =>
            add(key, label, options, defaultValue, v => new Bindable<Colour4>(v),
                e => readString(e) is string s && Colour4.TryParseHex(s, out var colour) ? colour : null,
                (setting, current) => new SettingsColour { LabelText = setting.Label, TooltipText = setting.Hint, Current = current },
                v => v.ToHex());

        public IBindable<string> File(string key, string label, IReadOnlyList<string>? extensions = null, SettingOptions? options = null) =>
            add(key, label, options, string.Empty, v => new Bindable<string>(v), readString,
                (setting, current) => fileSelector(setting, current, [.. extensions ?? []]));

        public void Button(string label, string buttonText, Action onClick) =>
            declared.Add(new ModSettingButton(manifest.Id, label, buttonText, onClick));

        public void Header(string text) => declared.Add(new ModSettingHeader(text));

        public void Note(string text) => declared.Add(new ModSettingNote(text));

        private static object? readString(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString() : null;

        // the selector works in FileInfo, mods get a plain path
        private static FormFileSelector fileSelector(ModSetting setting, Bindable<string> path, string[] extensions)
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

        private Bindable<T> add<T>(string key, string label, SettingOptions? options, T defaultValue, Func<T, Bindable<T>> create, Func<JsonElement, object?> read,
                                   Func<ModSetting, Bindable<T>, Drawable> control, Func<T, object>? save = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("a setting needs a key");

            if (declared.OfType<ModSetting>().Any(d => d.Key == key))
                throw new ArgumentException($"setting {key} is declared twice");

            T initial = defaultValue;

            lock (values)
            {
                if (values.TryGetValue(key, out var stored))
                {
                    if (read(stored) is T valid)
                        initial = valid;
                    else
                        LoaderLog.Info($"[{manifest.Id}] stored value for {key} doesn't fit anymore, using the default.");
                }
            }

            var setting = new ModSetting<T>(manifest.Id, key, label, options ?? new SettingOptions(), create, defaultValue, initial, control, v => set(key, save == null ? v : save(v)));
            declared.Add(setting);
            return setting.Live.GetBoundCopy();
        }

        private void set(string key, object? value)
        {
            lock (values)
                values[key] = JsonSerializer.SerializeToElement(value);

            saveTimer.Change(SAVE_DELAY, Timeout.InfiniteTimeSpan);
        }

        public void Flush()
        {
            Dictionary<string, JsonElement> snapshot;

            lock (values)
                snapshot = new Dictionary<string, JsonElement>(values);

            JsonFile.Write(path, snapshot);
        }

        public void Dispose() => saveTimer.Dispose();

        internal static void RaisePendingChanged() => PendingChanged?.Invoke();

        internal static string PathFor(string configDirectory, string id) => Path.Combine(configDirectory, $"{id}.json");

        // a mod's callback that throws turns the mod off instead of the manager's ui
        internal static void RunForMod(string modId, string what, Action action)
        {
            if (ModFaults.IsFaulted(modId))
                return;

            try
            {
                action();
            }
            catch (Exception e)
            {
                if (ModLoader.Get(modId) is { } mod)
                    ModFaults.Contain(mod, e, scheduler: null);
                else
                    LoaderLog.Error($"[{modId}] {what} threw.", e);
            }
        }

        internal static Container Padded(Drawable content) => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Padding = SettingsPanel.CONTENT_PADDING,
            Child = content,
        };
    }

    internal abstract class ModSettingElement
    {
        /// <summary>restart only and changed since launch</summary>
        public virtual bool Pending => false;

        public abstract Drawable CreateItem();
    }

    internal abstract class ModSetting(string key, string label, SettingOptions options) : ModSettingElement
    {
        public string Key { get; } = key;
        public string Label { get; } = label;
        public SettingOptions Options { get; } = options;

        // lazer shows the hint icon for any non-default string, even an empty one
        public LocalisableString Hint => Options.Description.Length > 0 ? Options.Description : default(LocalisableString);
    }

    /// <summary>
    ///     the player edits stored, the mod reads live, they only differ for restart only settings
    /// </summary>
    internal sealed class ModSetting<T> : ModSetting
    {
        private readonly T launchValue;
        private readonly Func<ModSetting, Bindable<T>, Drawable> control;

        public readonly Bindable<T> Stored;
        public readonly Bindable<T> Live;

        public override bool Pending => Options.RequiresRestart && !EqualityComparer<T>.Default.Equals(Stored.Value, launchValue);

        public ModSetting(string modId, string key, string label, SettingOptions options, Func<T, Bindable<T>> create, T defaultValue, T initial,
                          Func<ModSetting, Bindable<T>, Drawable> control, Action<T> save)
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
                    ModSettingsStore.RaisePendingChanged();
                else
                    ModSettingsStore.RunForMod(modId, $"setting {Key} changed handler", () => Live.Value = e.NewValue);
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
                    drawable is SettingsItem<T> ? drawable : ModSettingsStore.Padded(drawable),
                    ModSettingsStore.Padded(new SettingsNote { RelativeSizeAxes = Axes.X, Current = { BindTarget = note } }),
                ],
            };
        }
    }

    internal sealed class ModSettingButton(string modId, string label, string buttonText, Action onClick) : ModSettingElement
    {
        public override Drawable CreateItem() => ModSettingsStore.Padded(new FormButton
        {
            Caption = label,
            ButtonText = buttonText,
            Action = () => ModSettingsStore.RunForMod(modId, $"{label} button", onClick),
        });
    }

    internal sealed class ModSettingHeader(string text) : ModSettingElement
    {
        public override Drawable CreateItem() => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.GetFont(size: 20),
            Margin = new MarginPadding { Top = 8, Horizontal = 5 },
            Padding = SettingsPanel.CONTENT_PADDING,
        };
    }

    internal sealed class ModSettingNote(string text) : ModSettingElement
    {
        public override Drawable CreateItem() => ModSettingsStore.Padded(new SettingsNote
        {
            RelativeSizeAxes = Axes.X,
            Current = { Value = new SettingsNote.Data(text, SettingsNote.Type.Informational) },
        });
    }
}
