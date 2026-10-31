using System;
using System.Collections.Generic;
using Omamori.API;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets;
using osuTK;

namespace Omamori.Core.Config
{
    /// <summary>
    ///     click, then press the keys, escape cancels, delete or backspace unbinds
    /// </summary>
    internal sealed partial class KeyBindingControl : CompositeDrawable, IHasTooltip
    {
        private readonly string modId;
        private readonly string key;
        private readonly string label;
        private readonly Bindable<ModKeys> current;

        private RoundedButton button = null!;
        private OsuTextFlowContainer conflicts = null!;
        private List<KeyUse> lazerUses = [];
        private bool capturing;

        [Resolved]
        private ReadableKeyCombinationProvider readable { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public LocalisableString TooltipText { get; init; }

        public override bool AcceptsFocus => capturing;

        public KeyBindingControl(string modId, string key, string label, Bindable<ModKeys> current)
        {
            this.modId = modId;
            this.key = key;
            this.label = label;
            this.current = current;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void loadControl()
        {
            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children =
                [
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children =
                        [
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = label,
                            },
                            button = new RoundedButton
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Width = 200,
                                Action = startCapture,
                            },
                        ],
                    },
                    conflicts = new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 12))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Colour = colours.Yellow,
                    },
                ],
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            try
            {
                lazerUses = LazerKeyBindings.Read(realm, rulesets);
            }
            catch (Exception e)
            {
                LoaderLog.Error("couldn't read osu!'s key bindings for conflict checks.", e);
            }

            current.BindValueChanged(_ =>
            {
                showBinding();
                showConflicts();
            }, true);

            readable.KeymapChanged += onKeymapChanged;
        }

        // the keyboard layout can change from any thread
        private void onKeymapChanged() => Schedule(showBinding);

        private void showConflicts()
        {
            string? note = KeyConflicts.Note(KeyConflicts.Find(current.Value, modId, key, [.. lazerUses, .. ModConfig.KeyUses()]));

            conflicts.Text = note ?? string.Empty;
            conflicts.Alpha = note == null ? 0 : 1;
        }

        private void startCapture()
        {
            capturing = true;
            button.Text = "press keys...";
            GetContainingFocusManager()?.ChangeFocus(this);
        }

        private void stopCapture()
        {
            capturing = false;
            showBinding();
            GetContainingFocusManager()?.ChangeFocus(null);
        }

        private void showBinding()
        {
            if (capturing)
                return;

            button.Text = current.Value.IsNone ? "unbound" : readable.GetReadableString(current.Value.ToCombination());
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!capturing)
                return false;

            var pressed = KeyCombination.FromInputState(e.CurrentState);
            var trigger = KeyCombination.FromKey(e.Key);

            switch (KeyCapture.Decide(trigger, pressed))
            {
                case KeyCaptureResult.Cancel:
                    stopCapture();
                    break;

                case KeyCaptureResult.Clear:
                    current.Value = ModKeys.NONE;
                    stopCapture();
                    break;

                case KeyCaptureResult.Bind:
                    current.Value = KeyCapture.Combination(pressed, trigger);
                    stopCapture();
                    break;

                case KeyCaptureResult.Waiting:
                    button.Text = readable.GetReadableString(pressed);
                    break;
            }

            return true;
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            if (capturing)
            {
                capturing = false;
                showBinding();
            }

            base.OnFocusLost(e);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (readable != null)
                readable.KeymapChanged -= onKeymapChanged;

            base.Dispose(isDisposing);
        }
    }
}
