using System.Threading;
using System.Threading.Tasks;

namespace Omamori.API
{
    /// <summary>
    ///     what's inside the selected beatmap, copies that stay the same when osu! changes how it stores beatmaps
    ///     both methods do their work off the update thread, so await them rather than blocking on them
    ///     null before the game has finished loading, when nothing is selected, or when <see cref="Hook.Beatmaps"/> isn't available
    /// </summary>
    public interface IGameBeatmaps
    {
        /// <summary>
        ///     the selected beatmap as it plays with the selected ruleset and mods, the same objects a play would have
        ///     also null when the selected ruleset can't play it, like a mania map in osu!
        /// </summary>
        Task<BeatmapContent?> GetCurrentAsync(CancellationToken cancellationToken = default);

        /// <summary>osu!'s own numbers for the selected beatmap, ruleset and mods, from the same cache song select uses</summary>
        Task<DifficultyDetails?> GetCurrentDifficultyAsync(CancellationToken cancellationToken = default);
    }
}
