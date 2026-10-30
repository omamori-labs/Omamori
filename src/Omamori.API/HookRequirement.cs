namespace Omamori.API
{
    public enum HookRequirement
    {
        /// <summary>mod is skipped if the hook isn't available</summary>
        Required,

        /// <summary>mod loads anyway, only what uses the hook is skipped</summary>
        Optional,
    }
}
