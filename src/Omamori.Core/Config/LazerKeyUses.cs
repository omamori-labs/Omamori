using System;
using System.Collections.Generic;
using osu.Game.Database;
using osu.Game.Rulesets;

namespace Omamori.Core.Config
{
    /// <summary>
    ///     lazer's bindings read once and shared by every key binding control on the tab, update thread only
    /// </summary>
    internal sealed class LazerKeyUses
    {
        private IReadOnlyList<KeyUse>? uses;

        /// <summary>empty when they couldn't be read, conflict checks then only see other mods</summary>
        public IReadOnlyList<KeyUse> Get(RealmAccess realm, RulesetStore rulesets)
        {
            if (uses != null)
                return uses;

            try
            {
                uses = LazerKeyBindings.Read(realm, rulesets);
            }
            catch (Exception e)
            {
                LoaderLog.Error("couldn't read osu!'s key bindings for conflict checks.", e);
                uses = [];
            }

            return uses;
        }

        /// <summary>the next <see cref="Get"/> reads them again, for when the tab is rebuilt</summary>
        public void Forget() => uses = null;
    }
}
