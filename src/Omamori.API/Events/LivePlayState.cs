namespace Omamori.API
{
    public sealed record LivePlayState
    {
        public long TotalScore { get; init; }

        /// <summary>0 to 1</summary>
        public double Accuracy { get; init; }

        public int Combo { get; init; }

        public int MaxCombo { get; init; }

        /// <summary>0 to 1</summary>
        public double Health { get; init; }

        /// <summary>the grade the play is on track for</summary>
        public ScoreGrade Grade { get; init; }

        /// <summary>how many objects have been judged, goes down when a replay seeks backwards</summary>
        public int JudgedCount { get; init; }
    }
}
