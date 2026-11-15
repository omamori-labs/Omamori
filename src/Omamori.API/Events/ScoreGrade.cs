namespace Omamori.API
{
    /// <summary>
    ///     the letter a score gets, in order so grades compare, SH and XH are the silver ones from hidden or flashlight
    ///     <see cref="F"/> comes first so a grade nobody filled in never passes for a real one
    /// </summary>
    public enum ScoreGrade
    {
        F,
        D,
        C,
        B,
        A,
        S,
        SH,
        X,
        XH,
    }
}
