using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Game.Graphics.UserInterface;

namespace Omamori.Core.Manager
{
    /// <summary>
    ///     sheared toggles used as tabs, one per value with exactly one active
    /// </summary>
    internal static class ToggleGroup
    {
        /// <summary>
        ///     clicking the active button keeps it on, clicking another calls <paramref name="select"/>
        ///     keeping the buttons in step when <paramref name="current"/> changes is up to the caller
        /// </summary>
        public static ShearedToggleButton Button<T>(T value, IBindable<T> current, Action<T> select)
        {
            var comparer = EqualityComparer<T>.Default;
            var button = new ShearedToggleButton { Active = { Value = comparer.Equals(current.Value, value) } };

            button.Active.BindValueChanged(e =>
            {
                if (e.NewValue)
                    select(value);
                else if (comparer.Equals(current.Value, value))
                    button.Active.Value = true;
            });

            return button;
        }
    }
}
