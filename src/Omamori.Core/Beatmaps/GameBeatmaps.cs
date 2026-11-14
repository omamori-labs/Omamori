using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Omamori.API;
using Omamori.Core.Details;
using Omamori.Core.Features;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;

namespace Omamori.Core.Beatmaps
{
    internal sealed class GameBeatmaps : IGameBeatmaps
    {
        // set once the game has loaded
        private static OsuGame? game;

        private readonly ModManifest manifest;
        private readonly HookCheck hooks;

        public GameBeatmaps(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            hooks = new HookCheck(manifest, log);
        }

        public Task<BeatmapContent?> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            if (!selected(out _, out var working, out var ruleset, out var mods))
                return Task.FromResult<BeatmapContent?>(null);

            // converting a long map takes a while, so it never holds up a frame
            return Task.Run(() =>
            {
                try
                {
                    return GameDetails.Content(working.GetPlayableBeatmap(ruleset, mods, cancellationToken), working.BeatmapInfo, ruleset, mods);
                }
                catch (BeatmapInvalidForRulesetException)
                {
                    return null;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    LoaderLog.Error($"[{manifest.Id}] couldn't read {working.BeatmapInfo.DifficultyName}.", e);
                    return null;
                }
            }, cancellationToken);
        }

        public async Task<DifficultyDetails?> GetCurrentDifficultyAsync(CancellationToken cancellationToken = default)
        {
            if (!selected(out var g, out var working, out var ruleset, out var mods) || g.difficultyCache is not { } cache)
                return null;

            try
            {
                return await cache.GetDifficultyAsync(working.BeatmapInfo, ruleset, mods, cancellationToken).ConfigureAwait(false) is { } stars
                    ? new DifficultyDetails { StarRating = stars.Stars, MaxCombo = stars.MaxCombo, Ruleset = ruleset.ShortName }
                    : null;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                LoaderLog.Error($"[{manifest.Id}] couldn't work out the difficulty of {working.BeatmapInfo.DifficultyName}.", e);
                return null;
            }
        }

        public static void Attach(OsuGame loaded) => game = loaded;

        internal static void Clear() => game = null;

        // read on the caller's thread, the work that follows only uses what was read here
        private bool selected([NotNullWhen(true)] out OsuGame? g, [NotNullWhen(true)] out WorkingBeatmap? working, [NotNullWhen(true)] out RulesetInfo? ruleset, out IReadOnlyList<Mod> mods)
        {
            g = game;
            working = null;
            ruleset = null;
            mods = [];

            if (g == null || !hooks.CanRegister(Hook.Beatmaps, "beatmap read"))
                return false;

            working = g.Beatmap.Value;
            ruleset = g.Ruleset.Value;
            mods = g.SelectedMods.Value ?? [];

            return working is not null and not DummyWorkingBeatmap && ruleset is { Available: true };
        }
    }
}
