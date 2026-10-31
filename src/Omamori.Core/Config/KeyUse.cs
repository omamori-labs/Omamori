using Omamori.API;

namespace Omamori.Core.Config
{
    /// <summary>
    ///     a key binding someone else has, the owner is what the player sees like "osu!: Toggle chat",
    ///     setting is the mod setting it came from, null for lazer's own
    /// </summary>
    internal sealed record KeyUse(string Owner, ModKeys Keys, (string ModId, string Key)? Setting = null);
}
