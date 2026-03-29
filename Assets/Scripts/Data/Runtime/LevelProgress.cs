using System;

namespace StarFunc.Data
{
    [Serializable]
    public class LevelProgress
    {
        public bool IsCompleted;
        public int BestStars;
        public float BestTime;
        public int Attempts;
    }
}
