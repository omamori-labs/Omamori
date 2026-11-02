using System.ComponentModel;

namespace Omamori.NowPlaying
{
    public enum LargeImage
    {
        [Description("Beatmap cover")]
        Cover,

        [Description("osu! logo")]
        Osu,

        [Description("Custom image")]
        Custom,
    }
}
