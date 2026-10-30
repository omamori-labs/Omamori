using System;

namespace Omamori.API
{
    [Flags]
    public enum ReplayButtons
    {
        None = 0,
        Left1 = 1,
        Right1 = 2,
        Left2 = 4,
        Right2 = 8,
        Smoke = 16,
    }
}
