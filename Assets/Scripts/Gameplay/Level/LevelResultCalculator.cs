using StarFunc.Data;

namespace StarFunc.Gameplay
{
    /// <summary>
    /// Calculates the final result (star rating + fragment reward) for a completed level.
    /// Plain C# class — not a MonoBehaviour.
    /// </summary>
    public class LevelResultCalculator
    {
        /// <summary>
        /// Determine star rating from error count, elapsed time, and level thresholds.
        /// </summary>
        public LevelResult Calculate(LevelData level, int errors, float time)
        {
            var rating = level.StarRating;

            int stars;
            if (errors <= rating.ThreeStarMaxErrors)
                stars = 3;
            else if (errors <= rating.TwoStarMaxErrors)
                stars = 2;
            else if (errors <= rating.OneStarMaxErrors)
                stars = 1;
            else
                stars = 0;

            // Downgrade from 3 to 2 stars if time exceeds threshold.
            if (rating.TimerAffectsRating && stars == 3
                && rating.ThreeStarMaxTime > 0f && time > rating.ThreeStarMaxTime)
            {
                stars = 2;
            }

            int fragments = stars > 0 ? level.FragmentReward : 0;

            return new LevelResult
            {
                Stars = stars,
                Time = time,
                Errors = errors,
                FragmentsEarned = fragments
            };
        }
    }
}
