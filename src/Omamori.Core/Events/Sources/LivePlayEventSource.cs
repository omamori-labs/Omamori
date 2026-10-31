using Omamori.Core.Features;
using osu.Game;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     health applies before score, so both are current when the score processor raises its judgement
    /// </summary>
    internal static class LivePlayEventSource
    {
        public static void Watch(OsuGame game)
        {
            game.ScreenStack.ScreenPushed += (_, next) => FeatureRegistry.Run(Feature.LivePlayEvents, () =>
            {
                if (next is not Player player)
                    return;

                if (player.ScoreProcessor != null)
                    attach(player);
                else
                    player.OnLoadComplete += _ => FeatureRegistry.Run(Feature.LivePlayEvents, () => attach(player));
            });
        }

        private static void attach(Player player)
        {
            var score = player.ScoreProcessor;
            var health = player.HealthProcessor;
            var cursor = new HitCursor();

            score.NewJudgement += _ => FeatureRegistry.Run(Feature.LivePlayEvents, () =>
            {
                bool hits = GameEvents.HasHandlers(GameEvent.Hit);

                foreach (var hit in cursor.Unseen(score.HitEvents))
                {
                    if (hits)
                        GameEvents.Raise(GameEvent.Hit, EventDetails.Hit(hit));
                }

                raiseState(score, health);
            });

            score.JudgementReverted += _ => FeatureRegistry.Run(Feature.LivePlayEvents, () =>
            {
                cursor.Rewind(score.HitEvents.Count);
                raiseState(score, health);
            });

            raiseState(score, health);
        }

        private static void raiseState(ScoreProcessor score, HealthProcessor health)
        {
            if (GameEvents.HasHandlers(GameEvent.PlayStateChanged))
                GameEvents.Raise(GameEvent.PlayStateChanged, EventDetails.PlayState(score, health));
        }
    }
}
