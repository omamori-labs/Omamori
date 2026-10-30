using System;
using System.Collections.Generic;
using System.Threading;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UI;

namespace Omamori.Bootstrap
{
    /// <summary>
    ///     lazer creates every ruleset in rulesets/ on startup which loads omamori
    ///     still has to pass lazer's compat check so the factories return empty
    /// </summary>
    public class OmamoriRuleset : Ruleset
    {
        public const string SHORT_NAME = "omamori";

        public OmamoriRuleset()
        {
            Bootstrap.EnsureStarted(typeof(OmamoriRuleset).Assembly);
        }

        public override string Description => "Omamori mod loader";

        public override string ShortName => SHORT_NAME;

        public override string RulesetAPIVersionSupported => CURRENT_RULESET_API_VERSION;

        public override IEnumerable<Mod> GetModsFor(ModType type) => [];

        public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) => new HostBeatmapConverter(beatmap, this);

        public override DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap, IReadOnlyList<Mod>? mods = null) =>
            throw new NotSupportedException("The Omamori host ruleset is not playable.");

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) =>
            throw new NotSupportedException("The Omamori host ruleset is not playable.");

        private sealed class HostHitObject : HitObject
        {
        }

        private sealed class HostBeatmapConverter : BeatmapConverter<HostHitObject>
        {
            public HostBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
                : base(beatmap, ruleset)
            {
            }

            // if core fails to load the ruleset stays visible
            public override bool CanConvert() => false;

            protected override IEnumerable<HostHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken) => [];
        }
    }
}
