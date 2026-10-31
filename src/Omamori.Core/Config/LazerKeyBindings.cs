using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using osu.Framework.Extensions;
using osu.Game.Database;
using osu.Game.Input.Bindings;
using osu.Game.Rulesets;

namespace Omamori.Core.Config
{
    /// <summary>
    ///     lazer's own bindings from its realm, named like its key binding settings, a ruleset that won't load is left out
    /// </summary>
    internal static class LazerKeyBindings
    {
        public static List<KeyUse> Read(RealmAccess realm, RulesetStore rulesets)
        {
            var rows = realm.Run(r => r.All<RealmKeyBinding>().AsEnumerable()
                                       .Select(b => (b.RulesetName, b.Variant, b.ActionInt, Keys: b.KeyCombination))
                                       .ToList());

            var actionTypes = new Dictionary<(string Ruleset, int Variant), (string Owner, Type? Actions)>();
            var uses = new List<KeyUse>();

            foreach (var (rulesetName, variant, action, keys) in rows)
            {
                var modKeys = ModKeys.From(keys);

                if (modKeys.IsNone)
                    continue;

                if (string.IsNullOrEmpty(rulesetName))
                {
                    uses.Add(new KeyUse($"osu!: {((GlobalAction)action).GetLocalisableDescription()}", modKeys));
                    continue;
                }

                var group = (rulesetName, variant ?? 0);

                if (!actionTypes.TryGetValue(group, out var found))
                    actionTypes[group] = found = describe(rulesets, group.rulesetName, group.Item2);

                if (found.Actions != null)
                    uses.Add(new KeyUse($"{found.Owner}: {((Enum)Enum.ToObject(found.Actions, action)).GetLocalisableDescription()}", modKeys));
            }

            return uses;
        }

        private static (string Owner, Type? Actions) describe(RulesetStore rulesets, string shortName, int variant)
        {
            try
            {
                if (rulesets.GetRuleset(shortName) is not { } info)
                    return (shortName, null);

                var ruleset = info.CreateInstance();
                string variantName = variant == Ruleset.EDITOR_VARIANT ? string.Empty : ruleset.GetVariantName(variant).ToString();
                string kind = variant == Ruleset.EDITOR_VARIANT ? "editor" : "gameplay";
                string owner = variantName.Length > 0 ? $"{info.Name} {variantName} {kind}" : $"{info.Name} {kind}";

                return (owner, ruleset.GetDefaultKeyBindings(variant).FirstOrDefault()?.Action.GetType());
            }
            catch (Exception e)
            {
                LoaderLog.Info($"couldn't read {shortName}'s key bindings for conflict checks: {e.Message}");
                return (shortName, null);
            }
        }
    }
}
