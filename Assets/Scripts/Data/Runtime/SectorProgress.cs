using System;

namespace StarFunc.Data
{
    [Serializable]
    public class SectorProgress
    {
        public SectorState State;
        public int StarsCollected;
        public bool ControlLevelPassed;
    }
}
