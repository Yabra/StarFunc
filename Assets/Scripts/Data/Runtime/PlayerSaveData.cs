using System;
using System.Collections.Generic;

namespace StarFunc.Data
{
    [Serializable]
    public class PlayerSaveData
    {
        // Progression
        public Dictionary<string, SectorProgress> SectorProgress = new();
        public Dictionary<string, LevelProgress> LevelProgress = new();
        public int CurrentSectorIndex;

        // Economy
        public int TotalFragments;

        // Lives
        public int CurrentLives;
        public long LastLifeRestoreTimestamp;

        // Statistics
        public int TotalLevelsCompleted;
        public int TotalStarsCollected;
        public float TotalPlayTime;
    }
}
