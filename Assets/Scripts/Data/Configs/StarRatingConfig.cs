using System;

namespace StarFunc.Data
{
    [Serializable]
    public struct StarRatingConfig
    {
        public int ThreeStarMaxErrors;
        public int TwoStarMaxErrors;
        public int OneStarMaxErrors;
        public bool TimerAffectsRating;
        public float ThreeStarMaxTime;
    }
}
