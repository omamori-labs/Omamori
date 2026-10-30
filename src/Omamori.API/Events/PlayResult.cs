using System.Collections.Generic;

namespace Omamori.API
{
    public sealed record PlayResult
    {
        public PlayDetails Play { get; init; } = new PlayDetails();

        /// <summary>false when the player quit, failed and left, or retried</summary>
        public bool Completed { get; init; }

        /// <summary>only when <see cref="Completed"/></summary>
        public ScoreDetails? Score { get; init; }

        /// <summary>every judgement, only when <see cref="Completed"/>, for hit error graphs and the like</summary>
        public IReadOnlyList<HitDetails> Hits { get; init; } = [];

        /// <summary>the replay, only when <see cref="Completed"/>, for cursor paths and key visualisers after the map</summary>
        public IReadOnlyList<ReplayFrameDetails> Replay { get; init; } = [];
    }
}
