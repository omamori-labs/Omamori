namespace Omamori.API
{
    public sealed record LivePlayState
    {
        public long TotalScore { get; init; }

        /// <summary>0 to 1</summary>
        public double Accuracy { get; init; }

        public int Combo { get; init; }

        public int HighestCombo { get; init; }

        /// <summary>0 to 1</summary>
        public double Health { get; init; }

        /// <summary>D, C, B, A, S, SH, X, XH, the rank the play is on track for</summary>
        public string Rank { get; init; } = string.Empty;

        /// <summary>how many objects have been judged, goes down when a replay seeks backwards</summary>
        public int Judged { get; init; }
    }
}
